using BankFraudSystem.Data;
using BankFraudSystem.DTOs;
using BankFraudSystem.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BankFraudSystem.Services;

public interface IDashboardService
{
    Task<DashboardStatsResponse> GetStatsAsync();
    Task<IReadOnlyList<FraudAlertResponse>> GetAlertsAsync(bool unresolvedOnly);
    Task<FraudAlertResponse> ResolveAlertAsync(Guid alertId, string? note);
}

public class DashboardService(BankDbContext db) : IDashboardService
{
    public async Task<DashboardStatsResponse> GetStatsAsync()
    {
        var today = DateTime.UtcNow.Date;

        var totalAccounts      = await db.Accounts.CountAsync();
        var activeAccounts     = await db.Accounts.CountAsync(a => a.Status == AccountStatus.Active);
        var frozenAccounts     = await db.Accounts.CountAsync(a => a.Status == AccountStatus.Frozen);
        var totalTransactions  = await db.Transactions.CountAsync();
        var transactionsToday  = await db.Transactions.CountAsync(t => t.CreatedAt >= today);
        var totalVolumeToday   = await db.Transactions
            .Where(t => t.CreatedAt >= today && t.Status == TransactionStatus.Completed)
            .SumAsync(t => (decimal?)t.Amount) ?? 0;
        var fraudAlertsOpen    = await db.FraudAlerts.CountAsync(f => !f.IsResolved);
        var blockedToday       = await db.Transactions
            .CountAsync(t => t.CreatedAt >= today && t.Status == TransactionStatus.Blocked);

        return new DashboardStatsResponse(
            totalAccounts, activeAccounts, frozenAccounts,
            totalTransactions, transactionsToday, totalVolumeToday,
            fraudAlertsOpen, blockedToday);
    }

    public async Task<IReadOnlyList<FraudAlertResponse>> GetAlertsAsync(bool unresolvedOnly)
    {
        var query = db.FraudAlerts
            .Include(f => f.Account)
            .AsQueryable();

        if (unresolvedOnly)
            query = query.Where(f => !f.IsResolved);

        var alerts = await query
            .OrderByDescending(f => f.CreatedAt)
            .Take(100)
            .ToListAsync();

        return alerts.Select(ToAlertResponse).ToList();
    }

    public async Task<FraudAlertResponse> ResolveAlertAsync(Guid alertId, string? note)
    {
        var alert = await db.FraudAlerts
            .Include(f => f.Account)
            .FirstOrDefaultAsync(f => f.Id == alertId)
            ?? throw new KeyNotFoundException("Alerta não encontrado.");

        alert.IsResolved     = true;
        alert.ResolvedAt     = DateTime.UtcNow;
        alert.ResolutionNote = note;

        await db.SaveChangesAsync();
        return ToAlertResponse(alert);
    }

    private static FraudAlertResponse ToAlertResponse(FraudAlert alert)
    {
        var rules = JsonSerializer.Deserialize<List<string>>(alert.TriggeredRules) ?? new();
        return new FraudAlertResponse(
            alert.Id, alert.TransactionId, alert.AccountId,
            alert.Account.HolderName,
            alert.Score, alert.Severity, rules,
            alert.IsResolved, alert.ResolvedAt, alert.ResolutionNote,
            alert.CreatedAt);
    }
}
