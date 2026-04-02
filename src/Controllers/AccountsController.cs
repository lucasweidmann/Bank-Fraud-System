using BankFraudSystem.DTOs;
using BankFraudSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankFraudSystem.Controllers;

[ApiController]
[Route("api/accounts")]
public class AccountsController(IAccountService accountService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(AccountResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateAccountRequest request)
    {
        try
        {
            var account = await accountService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = account.Id }, account);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var response = await accountService.LoginAsync(request);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(AccountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            return Ok(await accountService.GetByIdAsync(id));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<AccountSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] string? q)
    {
        var results = await accountService.SearchAsync(q);
        return Ok(results);
    }

    [HttpPost("{id:guid}/freeze")]
    [Authorize]
    [ProducesResponseType(typeof(AccountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Freeze(Guid id, [FromBody] FreezeAccountRequest? request)
    {
        try
        {
            return Ok(await accountService.FreezeAsync(id));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{id:guid}/unfreeze")]
    [Authorize]
    [ProducesResponseType(typeof(AccountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unfreeze(Guid id)
    {
        try
        {
            return Ok(await accountService.UnfreezeAsync(id));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
