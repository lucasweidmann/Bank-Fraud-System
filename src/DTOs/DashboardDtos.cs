using BankFraudSystem.Models;

namespace BankFraudSystem.DTOs;

public record DashboardStatsResponse(
    int     TotalAccounts,
    int     ActiveAccounts,
    int     FrozenAccounts,
    int     TotalTransactions,
    int     TransactionsToday,
    decimal TotalVolumeToday,
    int     FraudAlertsOpen,
    int     BlockedToday
);

public record ResolveAlertRequest(string? Note);
