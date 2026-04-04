using BankFraudSystem.Data;
using BankFraudSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace BankFraudSystem.Services;

public record FraudResult(int Score, List<string> TriggeredRules);

public class FraudDetectionEngine(BankDbContext db)
{
    private static readonly HashSet<string> HighRiskCountries = new(StringComparer.OrdinalIgnoreCase)
    {
        "KP", "IR", "SY", "CU", "VE", "MM", "YE", "SD"
    };

    public async Task<FraudResult> AnalyzeAsync(Transaction tx, Account fromAccount)
    {
        var results = await Task.WhenAll(
            CheckVelocityAsync(tx, fromAccount),
            CheckUnusualAmountAsync(tx, fromAccount),
            CheckSuspiciousLocationAsync(tx),
            CheckNightActivityAsync(tx),
            CheckRoundAmountAsync(tx),
            CheckCrossBorderAsync(tx),
            CheckAccountTakeoverAsync(tx, fromAccount),
            CheckMultipleRecipientsAsync(tx, fromAccount)
        );

        var triggered = results.Where(r => r.score > 0).ToList();
        var baseScore = triggered.Sum(r => r.score);

        if (triggered.Count > 2)
            baseScore += (triggered.Count - 2) * 10;

        return new FraudResult(Math.Min(baseScore, 100), triggered.Select(r => r.rule).ToList());
    }

    private async Task<(int score, string rule)> CheckVelocityAsync(Transaction tx, Account from)
    {
        var since = tx.CreatedAt.AddMinutes(-10);
        var count = await db.Transactions
            .CountAsync(t => t.FromAccountId == from.Id &&
                             t.CreatedAt >= since &&
                             t.Id != tx.Id);

        return count > 5 ? (35, "VelocityExceeded") : (0, string.Empty);
    }

    private async Task<(int score, string rule)> CheckUnusualAmountAsync(Transaction tx, Account from)
    {
        var avg = await db.Transactions
            .Where(t => t.FromAccountId == from.Id &&
                        t.Status == TransactionStatus.Completed &&
                        t.Id != tx.Id)
            .AverageAsync(t => (double?)t.Amount) ?? 0;

        return avg > 0 && (double)tx.Amount > avg * 5
            ? (30, "UnusualAmount")
            : (0, string.Empty);
    }

    private async Task<(int score, string rule)> CheckSuspiciousLocationAsync(Transaction tx)
    {
        if (string.IsNullOrEmpty(tx.CountryCode))
            return (0, string.Empty);

        if (HighRiskCountries.Contains(tx.CountryCode))
            return (45, "SuspiciousLocation");

        var usedBefore = await db.Transactions
            .AnyAsync(t => t.FromAccountId == tx.FromAccountId &&
                           t.CountryCode == tx.CountryCode &&
                           t.Id != tx.Id);

        return !usedBefore ? (20, "SuspiciousLocation") : (0, string.Empty);
    }

    private Task<(int score, string rule)> CheckNightActivityAsync(Transaction tx)
    {
        var hour = tx.CreatedAt.Hour;
        if (hour is >= 1 and <= 2) return Task.FromResult((20, "NightActivity"));
        if (hour is >= 3 and <= 5) return Task.FromResult((10, "NightActivity"));
        return Task.FromResult((0, string.Empty));
    }

    private Task<(int score, string rule)> CheckRoundAmountAsync(Transaction tx)
    {
        var isRound = tx.Amount >= 10_000m && tx.Amount % 1000 == 0;
        return Task.FromResult(isRound ? (15, "RoundAmount") : (0, string.Empty));
    }

    private Task<(int score, string rule)> CheckCrossBorderAsync(Transaction tx)
    {
        var isCross = !string.IsNullOrEmpty(tx.CountryCode) &&
                      !tx.CountryCode.Equals("BR", StringComparison.OrdinalIgnoreCase) &&
                      tx.Amount > 50_000m;

        return Task.FromResult(isCross ? (20, "CrossBorderTransfer") : (0, string.Empty));
    }

    private Task<(int score, string rule)> CheckAccountTakeoverAsync(Transaction tx, Account from)
    {
        var accountAge = DateTime.UtcNow - from.CreatedAt;
        if (accountAge.TotalDays >= 7) return Task.FromResult((0, string.Empty));

        var score = tx.Amount switch
        {
            > 50_000m => 35,
            > 20_000m => 20,
            _         => 0
        };

        return Task.FromResult(score > 0 ? (score, "AccountTakeover") : (0, string.Empty));
    }

    private async Task<(int score, string rule)> CheckMultipleRecipientsAsync(Transaction tx, Account from)
    {
        var since = tx.CreatedAt.AddHours(-1);
        var distinctRecipients = await db.Transactions
            .Where(t => t.FromAccountId == from.Id &&
                        t.CreatedAt >= since &&
                        t.ToAccountId != null &&
                        t.Id != tx.Id)
            .Select(t => t.ToAccountId)
            .Distinct()
            .CountAsync();

        return distinctRecipients > 4
            ? (25, "MultipleRecipients")
            : (0, string.Empty);
    }
}
