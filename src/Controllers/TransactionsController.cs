using BankFraudSystem.DTOs;
using BankFraudSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankFraudSystem.Controllers;

[ApiController]
[Route("api/transactions")]
[Authorize]
public class TransactionsController(ITransactionService transactionService) : ControllerBase
{
    [HttpPost("transfer")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Transfer([FromBody] TransferRequest request)
    {
        var result = await transactionService.TransferAsync(request);
        return Ok(result);
    }

    [HttpPost("deposit")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Deposit([FromBody] DepositRequest request)
    {
        var result = await transactionService.DepositAsync(request);
        return Ok(result);
    }

    [HttpGet("history/{accountId:guid}")]
    [ProducesResponseType(typeof(TransactionHistoryResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> History(
        Guid accountId,
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 20)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page     = Math.Max(page, 1);

        var result = await transactionService.GetHistoryAsync(accountId, page, pageSize);
        return Ok(result);
    }
}
