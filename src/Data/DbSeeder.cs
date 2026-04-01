using BankFraudSystem.Data;
using BankFraudSystem.Models;
using BCrypt.Net;

namespace BankFraudSystem.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(BankDbContext db)
    {
        if (db.Accounts.Any()) return;

        var accounts = new[]
        {
            new Account
            {
                Id            = Guid.Parse("11111111-0000-0000-0000-000000000001"),
                HolderName    = "João Silva",
                Email         = "joao@example.com",
                PasswordHash  = BCrypt.Net.BCrypt.HashPassword("senha123"),
                AccountNumber = "0001-0",
                Balance       = 150_000m,
                CreatedAt     = DateTime.UtcNow.AddMonths(-6)
            },
            new Account
            {
                Id            = Guid.Parse("11111111-0000-0000-0000-000000000002"),
                HolderName    = "Maria Santos",
                Email         = "maria@example.com",
                PasswordHash  = BCrypt.Net.BCrypt.HashPassword("senha123"),
                AccountNumber = "0002-0",
                Balance       = 42_500m,
                CreatedAt     = DateTime.UtcNow.AddMonths(-4)
            },
            new Account
            {
                Id            = Guid.Parse("11111111-0000-0000-0000-000000000003"),
                HolderName    = "Empresa XPTO Ltda",
                Email         = "xpto@corp.com",
                PasswordHash  = BCrypt.Net.BCrypt.HashPassword("senha123"),
                AccountNumber = "0003-0",
                Balance       = 890_000m,
                CreatedAt     = DateTime.UtcNow.AddMonths(-12)
            },
            new Account
            {
                Id            = Guid.Parse("11111111-0000-0000-0000-000000000004"),
                HolderName    = "Carlos Oliveira",
                Email         = "carlos@example.com",
                PasswordHash  = BCrypt.Net.BCrypt.HashPassword("senha123"),
                AccountNumber = "0004-0",
                Balance       = 8_300m,
                CreatedAt     = DateTime.UtcNow.AddMonths(-2)
            },
            new Account
            {
                Id            = Guid.Parse("11111111-0000-0000-0000-000000000005"),
                HolderName    = "Ana Ferreira",
                Email         = "ana@example.com",
                PasswordHash  = BCrypt.Net.BCrypt.HashPassword("senha123"),
                AccountNumber = "0005-0",
                Balance       = 275_000m,
                CreatedAt     = DateTime.UtcNow.AddMonths(-8)
            }
        };

        db.Accounts.AddRange(accounts);
        await db.SaveChangesAsync();
    }
}
