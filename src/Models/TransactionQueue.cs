namespace BankFraudSystem.Models;

public class TransactionQueue
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TransactionId { get; set; }
    public DateTime EnqueuedAt { get; set; } = DateTime.UtcNow;
    public bool IsProcessing { get; set; }

    public Transaction Transaction { get; set; } = null!;
}
