using System.ComponentModel.DataAnnotations;
using BankFraudSystem.Models;

namespace BankFraudSystem.DTOs;

public record TransferRequest(
    [Required] Guid FromAccountId,
    [Required] Guid ToAccountId,
    [Range(0.01, double.MaxValue)] decimal Amount,
    string? Description,
    string? CountryCode
);

public record DepositRequest(
    [Required] Guid AccountId,
    [Range(0.01, double.MaxValue)] decimal Amount,
    string? Description
);

public record TransactionResponse(
    Guid              Id,
    Guid?             FromAccountId,
    string?           FromHolderName,
    Guid?             ToAccountId,
    string?           ToHolderName,
    decimal           Amount,
    TransactionType   Type,
    TransactionStatus Status,
    string?           Description,
    int               FraudScore,
    bool              IsFlagged,
    DateTime          CreatedAt,
    DateTime?         ProcessedAt,
    string?           FailureReason
);

public record FraudAlertResponse(
    Guid          Id,
    Guid          TransactionId,
    Guid          AccountId,
    string        AccountHolderName,
    int           Score,
    FraudSeverity Severity,
    List<string>  TriggeredRules,
    bool          IsResolved,
    DateTime?     ResolvedAt,
    string?       ResolutionNote,
    DateTime      CreatedAt
);

public record TransactionHistoryResponse(
    List<TransactionResponse> Items,
    int TotalCount,
    int Page,
    int PageSize
);
