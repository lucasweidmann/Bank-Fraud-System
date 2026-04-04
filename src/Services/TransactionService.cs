using System.Collections.Concurrent;
using System.Text.Json;
using BankFraudSystem.Data;
using BankFraudSystem.DTOs;
using BankFraudSystem.Hubs;
using BankFraudSystem.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BankFraudSystem.Services;

public interface ITransactionService
{
    Task<TransactionResponse> TransferAsync(TransferRequest request);
    Task<TransactionResponse> DepositAsync(DepositRequest request);
    Task<TransactionHistoryResponse> GetHistoryAsync(Guid accountId, int page, int pageSize);
}

public class TransactionService(
    BankDbContext db,
    FraudDetectionEngine fraudEngine,
    IHubContext<BankHub, IBankHubClient> hub,
    ILogger<TransactionService> logger) : ITransactionService
{
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> AccountLocks = new();

    public async Task<TransactionResponse> TransferAsync(TransferRequest request)
    {
        var fromAccount = await db.Accounts.FindAsync(request.FromAccountId)
            ?? throw new KeyNotFoundException("Conta de origem não encontrada.");

        var toAccount = await db.Accounts.FindAsync(request.ToAccountId)
            ?? throw new KeyNotFoundException("Conta de destino não encontrada.");

        if (fromAccount.Status == AccountStatus.Frozen)
            throw new InvalidOperationException("Conta de origem está congelada.");

        if (toAccount.Status == AccountStatus.Frozen)
            throw new InvalidOperationException("Conta de destino está congelada.");

        if (fromAccount.Id == toAccount.Id)
            throw new InvalidOperationException("Conta de origem e destino não podem ser a mesma.");

        var tx = new Transaction
        {
            FromAccountId = fromAccount.Id,
            ToAccountId   = toAccount.Id,
            Amount        = request.Amount,
            Type          = TransactionType.Transfer,
            Status        = TransactionStatus.Pending,
            Description   = request.Description,
            CountryCode   = request.CountryCode,
            CreatedAt     = DateTime.UtcNow
        };

        db.Transactions.Add(tx);
        await db.SaveChangesAsync();

        await hub.Clients.All.NewTransaction(ToResponse(tx, fromAccount, toAccount));

        var fraud = await fraudEngine.AnalyzeAsync(tx, fromAccount);
        tx.FraudScore = fraud.Score;
        tx.IsFlagged  = fraud.Score >= 40;

        logger.LogInformation("Transação {TxId} — score: {Score} regras: [{Rules}]",
            tx.Id, fraud.Score, string.Join(", ", fraud.TriggeredRules));

        if (fraud.Score >= 70)
        {
            tx.Status        = TransactionStatus.Blocked;
            tx.ProcessedAt   = DateTime.UtcNow;
            tx.FailureReason = $"Bloqueada por score de fraude: {fraud.Score}";
            await db.SaveChangesAsync();

            var alert = await CreateFraudAlertAsync(tx, fromAccount, fraud);

            if (fraud.Score >= 80)
            {
                fromAccount.Status   = AccountStatus.Frozen;
                fromAccount.FrozenAt = DateTime.UtcNow;
                await db.SaveChangesAsync();

                await hub.Clients.All.AccountFrozen(fromAccount.Id);
            }

            await hub.Clients.All.FraudDetected(new FraudDetectedPayload(
                ToResponse(tx, fromAccount, toAccount),
                ToAlertResponse(alert, fromAccount)));

            return ToResponse(tx, fromAccount, toAccount);
        }

        var accountLock = AccountLocks.GetOrAdd(fromAccount.Id, _ => new SemaphoreSlim(1, 1));
        await accountLock.WaitAsync();
        try
        {
            await db.Entry(fromAccount).ReloadAsync();

            if (fromAccount.Balance < request.Amount)
            {
                tx.Status        = TransactionStatus.Failed;
                tx.ProcessedAt   = DateTime.UtcNow;
                tx.FailureReason = "Saldo insuficiente.";
                await db.SaveChangesAsync();
                return ToResponse(tx, fromAccount, toAccount);
            }

            fromAccount.Balance -= request.Amount;
            toAccount.Balance   += request.Amount;
            tx.Status            = TransactionStatus.Completed;
            tx.ProcessedAt       = DateTime.UtcNow;

            if (fraud.Score >= 40)
                await CreateFraudAlertAsync(tx, fromAccount, fraud);

            await db.SaveChangesAsync();
        }
        finally
        {
            accountLock.Release();
        }

        var response = ToResponse(tx, fromAccount, toAccount);
        await hub.Clients.All.TransactionCompleted(response);

        if (tx.IsFlagged && tx.Status == TransactionStatus.Completed)
        {
            var flagAlert = await db.FraudAlerts.FirstOrDefaultAsync(f => f.TransactionId == tx.Id);
            if (flagAlert is not null)
                await hub.Clients.All.FraudDetected(new FraudDetectedPayload(
                    response, ToAlertResponse(flagAlert, fromAccount)));
        }

        return response;
    }

    public async Task<TransactionResponse> DepositAsync(DepositRequest request)
    {
        var account = await db.Accounts.FindAsync(request.AccountId)
            ?? throw new KeyNotFoundException("Conta não encontrada.");

        if (account.Status == AccountStatus.Frozen)
            throw new InvalidOperationException("Conta congelada.");

        var tx = new Transaction
        {
            ToAccountId = account.Id,
            Amount      = request.Amount,
            Type        = TransactionType.Deposit,
            Status      = TransactionStatus.Completed,
            Description = request.Description,
            ProcessedAt = DateTime.UtcNow,
            CreatedAt   = DateTime.UtcNow
        };

        account.Balance += request.Amount;
        db.Transactions.Add(tx);
        await db.SaveChangesAsync();

        var response = ToResponse(tx, null, account);
        await hub.Clients.All.TransactionCompleted(response);

        return response;
    }

    public async Task<TransactionHistoryResponse> GetHistoryAsync(Guid accountId, int page, int pageSize)
    {
        var query = db.Transactions
            .Include(t => t.FromAccount)
            .Include(t => t.ToAccount)
            .Where(t => t.FromAccountId == accountId || t.ToAccountId == accountId)
            .OrderByDescending(t => t.CreatedAt);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new TransactionHistoryResponse(
            items.Select(t => ToResponse(t, t.FromAccount, t.ToAccount)).ToList(),
            total, page, pageSize);
    }

    private async Task<FraudAlert> CreateFraudAlertAsync(Transaction tx, Account account, FraudResult fraud)
    {
        var severity = fraud.Score switch
        {
            >= 80 => FraudSeverity.Critical,
            >= 70 => FraudSeverity.High,
            _     => FraudSeverity.Low
        };

        var alert = new FraudAlert
        {
            TransactionId  = tx.Id,
            AccountId      = account.Id,
            Score          = fraud.Score,
            Severity       = severity,
            TriggeredRules = JsonSerializer.Serialize(fraud.TriggeredRules),
        };

        db.FraudAlerts.Add(alert);
        await db.SaveChangesAsync();
        return alert;
    }

    private static TransactionResponse ToResponse(Transaction tx, Account? from, Account? to) => new(
        tx.Id,
        tx.FromAccountId, from?.HolderName,
        tx.ToAccountId,   to?.HolderName,
        tx.Amount, tx.Type, tx.Status,
        tx.Description,
        tx.FraudScore, tx.IsFlagged,
        tx.CreatedAt, tx.ProcessedAt,
        tx.FailureReason);

    private static FraudAlertResponse ToAlertResponse(FraudAlert alert, Account account)
    {
        var rules = JsonSerializer.Deserialize<List<string>>(alert.TriggeredRules) ?? new();
        return new FraudAlertResponse(
            alert.Id, alert.TransactionId, alert.AccountId,
            account.HolderName,
            alert.Score, alert.Severity, rules,
            alert.IsResolved, alert.ResolvedAt, alert.ResolutionNote,
            alert.CreatedAt);
    }
}
