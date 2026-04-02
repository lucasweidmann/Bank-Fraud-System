using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BankFraudSystem.Data;
using BankFraudSystem.DTOs;
using BankFraudSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace BankFraudSystem.Services;

public interface IAccountService
{
    Task<AccountResponse>  CreateAsync(CreateAccountRequest request);
    Task<LoginResponse>    LoginAsync(LoginRequest request);
    Task<AccountResponse>  GetByIdAsync(Guid id);
    Task<IReadOnlyList<AccountSummaryResponse>> SearchAsync(string? query);
    Task<AccountResponse>  FreezeAsync(Guid id);
    Task<AccountResponse>  UnfreezeAsync(Guid id);
}

public class AccountService(BankDbContext db, IConfiguration config) : IAccountService
{
    public async Task<AccountResponse> CreateAsync(CreateAccountRequest request)
    {
        if (await db.Accounts.AnyAsync(a => a.Email == request.Email))
            throw new InvalidOperationException("E-mail já cadastrado.");

        var account = new Account
        {
            HolderName    = request.HolderName,
            Email         = request.Email,
            PasswordHash  = BCrypt.Net.BCrypt.HashPassword(request.Password),
            AccountNumber = await GenerateAccountNumberAsync(),
            Balance       = request.InitialBalance,
        };

        db.Accounts.Add(account);
        await db.SaveChangesAsync();

        return ToResponse(account);
    }
    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var account = await db.Accounts.FirstOrDefaultAsync(a => a.Email == request.Email)
            ?? throw new UnauthorizedAccessException("Credenciais inválidas.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, account.PasswordHash))
            throw new UnauthorizedAccessException("Credenciais inválidas.");

        if (account.Status == AccountStatus.Frozen)
            throw new InvalidOperationException("Conta congelada. Entre em contato com o suporte.");

        var (token, expiresAt) = GenerateJwt(account);
        return new LoginResponse(token, expiresAt, ToResponse(account));
    }
    public async Task<AccountResponse> GetByIdAsync(Guid id)
    {
        var account = await db.Accounts.FindAsync(id)
            ?? throw new KeyNotFoundException("Conta não encontrada.");
        return ToResponse(account);
    }
    public async Task<IReadOnlyList<AccountSummaryResponse>> SearchAsync(string? query)
    {
        var q = db.Accounts.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
            q = q.Where(a =>
                a.HolderName.ToLower().Contains(query.ToLower()) ||
                a.AccountNumber.Contains(query));

        return await q
            .OrderBy(a => a.HolderName)
            .Take(50)
            .Select(a => new AccountSummaryResponse(
                a.Id, a.HolderName, a.AccountNumber, a.Status, a.Balance))
            .ToListAsync();
    }
    public async Task<AccountResponse> FreezeAsync(Guid id)
    {
        var account = await db.Accounts.FindAsync(id)
            ?? throw new KeyNotFoundException("Conta não encontrada.");

        account.Status   = AccountStatus.Frozen;
        account.FrozenAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return ToResponse(account);
    }
    public async Task<AccountResponse> UnfreezeAsync(Guid id)
    {
        var account = await db.Accounts.FindAsync(id)
            ?? throw new KeyNotFoundException("Conta não encontrada.");

        account.Status   = AccountStatus.Active;
        account.FrozenAt = null;
        await db.SaveChangesAsync();

        return ToResponse(account);
    }
    private async Task<string> GenerateAccountNumberAsync()
    {
        var count = await db.Accounts.CountAsync();
        return $"{(count + 1):D4}-{Random.Shared.Next(0, 9)}";
    }

    private (string token, DateTime expiresAt) GenerateJwt(Account account)
    {
        var jwtConfig  = config.GetSection("Jwt");
        var secret     = jwtConfig["Secret"] ?? throw new InvalidOperationException("JWT Secret não configurado.");
        var key        = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds      = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt  = DateTime.UtcNow.AddHours(double.Parse(jwtConfig["ExpirationHours"] ?? "8"));

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   account.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, account.Email),
            new Claim("holderName",                  account.HolderName),
            new Claim("accountNumber",               account.AccountNumber),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer:             jwtConfig["Issuer"],
            audience:           jwtConfig["Audience"],
            claims:             claims,
            expires:            expiresAt,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    private static AccountResponse ToResponse(Account a) => new(
        a.Id, a.HolderName, a.Email, a.AccountNumber,
        a.Balance, a.Status, a.CreatedAt, a.FrozenAt);
}
