using BankFraudSystem.DTOs;
using BankFraudSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankFraudSystem.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    [HttpGet("stats")]
    [ProducesResponseType(typeof(DashboardStatsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Stats()
        => Ok(await dashboardService.GetStatsAsync());

    [HttpGet("alerts")]
    [ProducesResponseType(typeof(IReadOnlyList<FraudAlertResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Alerts([FromQuery] bool unresolvedOnly = false)
        => Ok(await dashboardService.GetAlertsAsync(unresolvedOnly));

    [HttpPost("alerts/{id:guid}/resolve")]
    [ProducesResponseType(typeof(FraudAlertResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Resolve(Guid id, [FromBody] ResolveAlertRequest? request)
    {
        try
        {
            return Ok(await dashboardService.ResolveAlertAsync(id, request?.Note));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
