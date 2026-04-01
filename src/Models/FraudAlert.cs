using System.ComponentModel.DataAnnotations;

namespace BankFraudSystem.Models;

public class FraudAlert
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TransactionId { get; set; }
    public Guid AccountId { get; set; }

    public int Score { get; set; }
    public FraudSeverity Severity { get; set; }

    [Required]
    public string TriggeredRules { get; set; } = string.Empty;

    public bool IsResolved { get; set; }
    public DateTime? ResolvedAt { get; set; }

    [MaxLength(500)]
    public string? ResolutionNote { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Transaction Transaction { get; set; } = null!;
    public Account Account { get; set; } = null!;
}

public enum FraudSeverity
{
    Low = 0,
    High = 1,
    Critical = 2
}
