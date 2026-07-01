using CvSU.Ais.Api.Auth;
using CvSU.Ais.Application.CashAdvances;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CvSU.Ais.Api.Controllers;

[ApiController]
[Route("api/cash-advances")]
[Authorize]
public sealed class CashAdvancesController(CashAdvanceService service) : ControllerBase
{
    public sealed record CreateCashAdvanceRequest(
        string Employee,
        string EmployeeName,
        DateOnly PostingDate,
        string? FundCluster,
        string Purpose,
        decimal AdvanceAmount,
        DateOnly DueDate,
        string? Remarks = null);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CashAdvanceView>>> List(
        CancellationToken cancellationToken) =>
        Ok(await service.ListAsync(cancellationToken));

    [HttpPost]
    [Authorize(Policy = CashAdvancePolicies.Manage)]
    public async Task<ActionResult<CashAdvanceDetailView>> Create(
        CreateCashAdvanceRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateCashAdvanceCommand(
            Employee: request.Employee,
            EmployeeName: request.EmployeeName,
            PostingDate: request.PostingDate,
            FundCluster: request.FundCluster,
            Purpose: request.Purpose,
            AdvanceAmount: request.AdvanceAmount,
            DueDate: request.DueDate,
            Remarks: request.Remarks);

        var name = await service.CreateAsync(command, cancellationToken);
        var detail = await service.GetAsync(name, cancellationToken);
        return CreatedAtAction(nameof(Get), new { name }, detail);
    }

    [HttpGet("{name}")]
    public async Task<ActionResult<CashAdvanceDetailView>> Get(
        string name,
        CancellationToken cancellationToken) =>
        Ok(await service.GetAsync(name, cancellationToken));

    [HttpPost("{name}/advance")]
    [Authorize(Policy = CashAdvancePolicies.Disburse)]
    public async Task<IActionResult> Advance(
        string name,
        CancellationToken cancellationToken)
    {
        await service.AdvanceAsync(name, cancellationToken);
        return Ok();
    }

    [HttpPost("{name}/cancel")]
    [Authorize(Policy = CashAdvancePolicies.Manage)]
    public async Task<IActionResult> Cancel(
        string name,
        CancellationToken cancellationToken)
    {
        await service.CancelAsync(name, cancellationToken);
        return Ok();
    }
}
