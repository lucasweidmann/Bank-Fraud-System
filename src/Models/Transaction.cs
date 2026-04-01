using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankFraudSystem.Models;

public class Transaction
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? FromAccountId { get; set; }
    public Guid? ToAccountId { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    public TransactionType Type { get; set; }
    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(100)]
    public string? CountryCode { get; set; }

    public int FraudScore { get; set; }
    public bool IsFlagged { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }

    [MaxLength(500)]
    public string? FailureReason { get; set; }

    public Account? FromAccount { get; set; }
    public Account? ToAccount { get; set; }
    public FraudAlert? FraudAlert { get; set; }
}

public enum TransactionType
{
    Deposit = 0,
    Transfer = 1,
    Withdrawal = 2
}

public enum TransactionStatus
{
    Pending = 0,
    Completed = 1,
    Failed = 2,
    Blocked = 3
}
