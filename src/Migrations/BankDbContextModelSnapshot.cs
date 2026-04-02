using System;
using BankFraudSystem.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BankFraudSystem.Migrations
{
    [DbContext(typeof(BankDbContext))]
    partial class BankDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.4")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("BankFraudSystem.Models.Account", b =>
                {
                    b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uuid");
                    b.Property<string>("AccountNumber").IsRequired().HasMaxLength(20).HasColumnType("character varying(20)");
                    b.Property<decimal>("Balance").HasPrecision(18, 2).HasColumnType("numeric(18,2)");
                    b.Property<DateTime>("CreatedAt").HasColumnType("timestamp with time zone");
                    b.Property<string>("Email").IsRequired().HasMaxLength(150).HasColumnType("character varying(150)");
                    b.Property<DateTime?>("FrozenAt").HasColumnType("timestamp with time zone");
                    b.Property<string>("HolderName").IsRequired().HasMaxLength(100).HasColumnType("character varying(100)");
                    b.Property<string>("PasswordHash").IsRequired().HasColumnType("text");
                    b.Property<int>("Status").HasColumnType("integer");
                    b.HasKey("Id");
                    b.HasIndex("AccountNumber").IsUnique();
                    b.HasIndex("Email").IsUnique();
                    b.ToTable("Accounts");
                });

            modelBuilder.Entity("BankFraudSystem.Models.FraudAlert", b =>
                {
                    b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uuid");
                    b.Property<Guid>("AccountId").HasColumnType("uuid");
                    b.Property<DateTime>("CreatedAt").HasColumnType("timestamp with time zone");
                    b.Property<bool>("IsResolved").HasColumnType("boolean");
                    b.Property<string>("ResolutionNote").HasMaxLength(500).HasColumnType("character varying(500)");
                    b.Property<DateTime?>("ResolvedAt").HasColumnType("timestamp with time zone");
                    b.Property<int>("Score").HasColumnType("integer");
                    b.Property<int>("Severity").HasColumnType("integer");
                    b.Property<Guid>("TransactionId").HasColumnType("uuid");
                    b.Property<string>("TriggeredRules").IsRequired().HasColumnType("text");
                    b.HasKey("Id");
                    b.HasIndex("AccountId");
                    b.HasIndex("CreatedAt");
                    b.HasIndex("IsResolved");
                    b.HasIndex("TransactionId").IsUnique();
                    b.ToTable("FraudAlerts");
                });

            modelBuilder.Entity("BankFraudSystem.Models.Transaction", b =>
                {
                    b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uuid");
                    b.Property<decimal>("Amount").HasPrecision(18, 2).HasColumnType("numeric(18,2)");
                    b.Property<string>("CountryCode").HasMaxLength(100).HasColumnType("character varying(100)");
                    b.Property<DateTime>("CreatedAt").HasColumnType("timestamp with time zone");
                    b.Property<string>("Description").HasMaxLength(500).HasColumnType("character varying(500)");
                    b.Property<string>("FailureReason").HasMaxLength(500).HasColumnType("character varying(500)");
                    b.Property<Guid?>("FromAccountId").HasColumnType("uuid");
                    b.Property<int>("FraudScore").HasColumnType("integer");
                    b.Property<bool>("IsFlagged").HasColumnType("boolean");
                    b.Property<DateTime?>("ProcessedAt").HasColumnType("timestamp with time zone");
                    b.Property<int>("Status").HasColumnType("integer");
                    b.Property<Guid?>("ToAccountId").HasColumnType("uuid");
                    b.Property<int>("Type").HasColumnType("integer");
                    b.HasKey("Id");
                    b.HasIndex("CreatedAt");
                    b.HasIndex("FromAccountId");
                    b.ToTable("Transactions");
                });

            modelBuilder.Entity("BankFraudSystem.Models.TransactionQueue", b =>
                {
                    b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uuid");
                    b.Property<DateTime>("EnqueuedAt").HasColumnType("timestamp with time zone");
                    b.Property<bool>("IsProcessing").HasColumnType("boolean");
                    b.Property<Guid>("TransactionId").HasColumnType("uuid");
                    b.HasKey("Id");
                    b.HasIndex("EnqueuedAt");
                    b.HasIndex("TransactionId");
                    b.ToTable("TransactionQueues");
                });

            modelBuilder.Entity("BankFraudSystem.Models.FraudAlert", b =>
                {
                    b.HasOne("BankFraudSystem.Models.Account", "Account")
                     .WithMany("FraudAlerts")
                     .HasForeignKey("AccountId")
                     .OnDelete(DeleteBehavior.Cascade)
                     .IsRequired();
                    b.HasOne("BankFraudSystem.Models.Transaction", "Transaction")
                     .WithOne("FraudAlert")
                     .HasForeignKey<BankFraudSystem.Models.FraudAlert>("TransactionId")
                     .OnDelete(DeleteBehavior.Cascade)
                     .IsRequired();
                    b.Navigation("Account");
                    b.Navigation("Transaction");
                });

            modelBuilder.Entity("BankFraudSystem.Models.Transaction", b =>
                {
                    b.HasOne("BankFraudSystem.Models.Account", "FromAccount")
                     .WithMany("SentTransactions")
                     .HasForeignKey("FromAccountId")
                     .OnDelete(DeleteBehavior.Restrict);
                    b.HasOne("BankFraudSystem.Models.Account", "ToAccount")
                     .WithMany("ReceivedTransactions")
                     .HasForeignKey("ToAccountId")
                     .OnDelete(DeleteBehavior.Restrict);
                    b.Navigation("FromAccount");
                    b.Navigation("ToAccount");
                });

            modelBuilder.Entity("BankFraudSystem.Models.TransactionQueue", b =>
                {
                    b.HasOne("BankFraudSystem.Models.Transaction", "Transaction")
                     .WithMany()
                     .HasForeignKey("TransactionId")
                     .OnDelete(DeleteBehavior.Cascade)
                     .IsRequired();
                    b.Navigation("Transaction");
                });

            modelBuilder.Entity("BankFraudSystem.Models.Account", b =>
                {
                    b.Navigation("FraudAlerts");
                    b.Navigation("ReceivedTransactions");
                    b.Navigation("SentTransactions");
                });

            modelBuilder.Entity("BankFraudSystem.Models.Transaction", b =>
                {
                    b.Navigation("FraudAlert");
                });
#pragma warning restore 612, 618
        }
    }
}
