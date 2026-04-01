using BankFraudSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace BankFraudSystem.Data;

public class BankDbContext(DbContextOptions<BankDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<FraudAlert> FraudAlerts => Set<FraudAlert>();
    public DbSet<TransactionQueue> TransactionQueues => Set<TransactionQueue>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Account>(e =>
        {
            e.HasIndex(a => a.Email).IsUnique();
            e.HasIndex(a => a.AccountNumber).IsUnique();
            e.Property(a => a.Balance).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Transaction>(e =>
        {
            e.HasOne(t => t.FromAccount)
             .WithMany(a => a.SentTransactions)
             .HasForeignKey(t => t.FromAccountId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(t => t.ToAccount)
             .WithMany(a => a.ReceivedTransactions)
             .HasForeignKey(t => t.ToAccountId)
             .OnDelete(DeleteBehavior.Restrict);

            e.Property(t => t.Amount).HasPrecision(18, 2);
            e.HasIndex(t => t.CreatedAt);
            e.HasIndex(t => t.FromAccountId);
        });

        modelBuilder.Entity<FraudAlert>(e =>
        {
            e.HasOne(f => f.Transaction)
             .WithOne(t => t.FraudAlert)
             .HasForeignKey<FraudAlert>(f => f.TransactionId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(f => f.Account)
             .WithMany(a => a.FraudAlerts)
             .HasForeignKey(f => f.AccountId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(f => f.IsResolved);
            e.HasIndex(f => f.CreatedAt);
        });

        modelBuilder.Entity<TransactionQueue>(e =>
        {
            e.HasOne(q => q.Transaction)
             .WithMany()
             .HasForeignKey(q => q.TransactionId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(q => q.EnqueuedAt);
        });
    }
}
