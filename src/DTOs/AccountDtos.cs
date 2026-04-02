using System.ComponentModel.DataAnnotations;
using BankFraudSystem.Models;

namespace BankFraudSystem.DTOs;

public record CreateAccountRequest(
    [Required, MaxLength(100)] string HolderName,
    [Required, EmailAddress]   string Email,
    [Required, MinLength(6)]   string Password,
    [Range(0, double.MaxValue)] decimal InitialBalance = 0
);

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required]               string Password
);

public record FreezeAccountRequest(string? Reason);

public record AccountResponse(
    Guid          Id,
    string        HolderName,
    string        Email,
    string        AccountNumber,
    decimal       Balance,
    AccountStatus Status,
    DateTime      CreatedAt,
    DateTime?     FrozenAt
);

public record LoginResponse(
    string        Token,
    DateTime      ExpiresAt,
    AccountResponse Account
);

public record AccountSummaryResponse(
    Guid          Id,
    string        HolderName,
    string        AccountNumber,
    AccountStatus Status,
    decimal       Balance
);
