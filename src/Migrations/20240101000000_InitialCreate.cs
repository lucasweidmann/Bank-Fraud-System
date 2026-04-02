using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankFraudSystem.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id            = table.Column<Guid>(type: "uuid", nullable: false),
                    HolderName    = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email         = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    PasswordHash  = table.Column<string>(type: "text", nullable: false),
                    AccountNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Balance       = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Status        = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt     = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FrozenAt      = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_Accounts", x => x.Id));

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    Id            = table.Column<Guid>(type: "uuid", nullable: false),
                    FromAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    ToAccountId   = table.Column<Guid>(type: "uuid", nullable: true),
                    Amount        = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Type          = table.Column<int>(type: "integer", nullable: false),
                    Status        = table.Column<int>(type: "integer", nullable: false),
                    Description   = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CountryCode   = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FraudScore    = table.Column<int>(type: "integer", nullable: false),
                    IsFlagged     = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt     = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt   = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                    table.ForeignKey("FK_Transactions_Accounts_FromAccountId", x => x.FromAccountId, "Accounts", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_Transactions_Accounts_ToAccountId",   x => x.ToAccountId,   "Accounts", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FraudAlerts",
                columns: table => new
                {
                    Id             = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionId  = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId      = table.Column<Guid>(type: "uuid", nullable: false),
                    Score          = table.Column<int>(type: "integer", nullable: false),
                    Severity       = table.Column<int>(type: "integer", nullable: false),
                    TriggeredRules = table.Column<string>(type: "text", nullable: false),
                    IsResolved     = table.Column<bool>(type: "boolean", nullable: false),
                    ResolvedAt     = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolutionNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt      = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FraudAlerts", x => x.Id);
                    table.ForeignKey("FK_FraudAlerts_Accounts_AccountId",         x => x.AccountId,     "Accounts",     "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_FraudAlerts_Transactions_TransactionId", x => x.TransactionId, "Transactions", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TransactionQueues",
                columns: table => new
                {
                    Id            = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnqueuedAt    = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsProcessing  = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionQueues", x => x.Id);
                    table.ForeignKey("FK_TransactionQueues_Transactions_TransactionId", x => x.TransactionId, "Transactions", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex("IX_Accounts_Email",         "Accounts",         "Email",         unique: true);
            migrationBuilder.CreateIndex("IX_Accounts_AccountNumber", "Accounts",         "AccountNumber", unique: true);
            migrationBuilder.CreateIndex("IX_Transactions_CreatedAt",     "Transactions",     "CreatedAt");
            migrationBuilder.CreateIndex("IX_Transactions_FromAccountId", "Transactions",     "FromAccountId");
            migrationBuilder.CreateIndex("IX_FraudAlerts_AccountId",     "FraudAlerts",      "AccountId");
            migrationBuilder.CreateIndex("IX_FraudAlerts_TransactionId", "FraudAlerts",      "TransactionId", unique: true);
            migrationBuilder.CreateIndex("IX_FraudAlerts_IsResolved",    "FraudAlerts",      "IsResolved");
            migrationBuilder.CreateIndex("IX_FraudAlerts_CreatedAt",     "FraudAlerts",      "CreatedAt");
            migrationBuilder.CreateIndex("IX_TransactionQueues_EnqueuedAt",    "TransactionQueues", "EnqueuedAt");
            migrationBuilder.CreateIndex("IX_TransactionQueues_TransactionId", "TransactionQueues", "TransactionId");
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "TransactionQueues");
            migrationBuilder.DropTable(name: "FraudAlerts");
            migrationBuilder.DropTable(name: "Transactions");
            migrationBuilder.DropTable(name: "Accounts");
        }
    }
}
