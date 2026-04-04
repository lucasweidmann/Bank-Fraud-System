using BankFraudSystem.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BankFraudSystem.Hubs;

[Authorize]
public class BankHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}

public interface IBankHubClient
{
    Task NewTransaction(TransactionResponse transaction);
    Task TransactionCompleted(TransactionResponse transaction);
    Task FraudDetected(FraudDetectedPayload payload);
    Task AccountFrozen(Guid accountId);
}

public record FraudDetectedPayload(
    TransactionResponse Transaction,
    FraudAlertResponse  FraudAlert
);
