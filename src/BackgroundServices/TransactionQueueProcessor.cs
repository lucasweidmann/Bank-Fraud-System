using BankFraudSystem.Data;
using BankFraudSystem.Models;
using BankFraudSystem.Services;
using Microsoft.EntityFrameworkCore;

namespace BankFraudSystem.BackgroundServices;

public class TransactionQueueProcessor(
    IServiceScopeFactory scopeFactory,
    ILogger<TransactionQueueProcessor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("TransactionQueueProcessor iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro no TransactionQueueProcessor.");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }

    private async Task ProcessPendingAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BankDbContext>();

        var pending = await db.TransactionQueues
            .Include(q => q.Transaction)
            .Where(q => !q.IsProcessing)
            .OrderBy(q => q.EnqueuedAt)
            .Take(10)
            .ToListAsync(ct);

        if (pending.Count == 0) return;

        foreach (var item in pending)
        {
            item.IsProcessing = true;
        }
        await db.SaveChangesAsync(ct);

        foreach (var item in pending)
        {
            try
            {
                if (item.Transaction.Status == TransactionStatus.Pending)
                {
                    item.Transaction.Status      = TransactionStatus.Failed;
                    item.Transaction.ProcessedAt = DateTime.UtcNow;
                    item.Transaction.FailureReason = "Expirada na fila sem processamento.";

                    logger.LogWarning("Transação {TxId} expirada na fila.", item.TransactionId);
                }

                db.TransactionQueues.Remove(item);
                await db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao processar item {QueueId} da fila.", item.Id);
                item.IsProcessing = false;
                await db.SaveChangesAsync(ct);
            }
        }
    }
}
