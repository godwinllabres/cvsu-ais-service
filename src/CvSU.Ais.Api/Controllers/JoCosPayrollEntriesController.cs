using CvSU.Ais.Api.Auth;
using CvSU.Ais.Application.Payroll;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CvSU.Ais.Api.Controllers;

[ApiController]
[Route("api/jo-cos-payroll-entries")]
[Authorize]
public sealed class JoCosPayrollEntriesController(JoCosPayrollService service) : ControllerBase
{
    public sealed record CreateJoCosPayrollRequest(
        string EmployeeType,
        string PayrollPeriod,
        DateOnly? PeriodFrom,
        DateOnly? PeriodTo,
        DateOnly PostingDate,
        string? FundCluster,
        IReadOnlyList<JoCosPayrollLineDto>? Lines,
        string? Remarks);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<JoCosPayrollView>>> List(CancellationToken cancellationToken) =>
        Ok(await service.ListAsync(cancellationToken));

    [HttpPost]
    [Authorize(Policy = PayrollPolicies.Manage)]
    public async Task<ActionResult<JoCosPayrollView>> Create(
        CreateJoCosPayrollRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateJoCosPayrollCommand(
            request.EmployeeType,
            request.PayrollPeriod,
            request.PeriodFrom,
            request.PeriodTo,
            request.PostingDate,
            request.FundCluster,
            request.Lines ?? [],
            request.Remarks);

        var view = await service.CreateAsync(command, cancellationToken);
        return CreatedAtAction(nameof(Get), new { name = view.Name }, view);
    }

    [HttpGet("{name}")]
    public async Task<ActionResult<JoCosPayrollDetailView>> Get(string name, CancellationToken cancellationToken) =>
        Ok(await service.GetAsync(name, cancellationToken));

    [HttpPost("{name}/validate-hours")]
    [Authorize(Policy = PayrollPolicies.Manage)]
    public async Task<IActionResult> ValidateHours(string name, CancellationToken cancellationToken)
    {
        await service.ValidateHoursAsync(name, cancellationToken);
        return Ok();
    }

    [HttpPost("{name}/compute")]
    [Authorize(Policy = PayrollPolicies.Manage)]
    public async Task<IActionResult> Compute(string name, CancellationToken cancellationToken)
    {
        await service.ComputeAsync(name, cancellationToken);
        return Ok();
    }

    [HttpPost("{name}/approve")]
    [Authorize(Policy = PayrollPolicies.Manage)]
    public async Task<IActionResult> Approve(string name, CancellationToken cancellationToken)
    {
        await service.ApproveAsync(name, cancellationToken);
        return Ok();
    }

    [HttpPost("{name}/post")]
    [Authorize(Policy = PayrollPolicies.Post)]
    public async Task<IActionResult> Post(string name, CancellationToken cancellationToken)
    {
        await service.PostAsync(name, cancellationToken);
        return Ok();
    }

    [HttpPost("{name}/cancel")]
    [Authorize(Policy = PayrollPolicies.Manage)]
    public async Task<IActionResult> Cancel(string name, CancellationToken cancellationToken)
    {
        await service.CancelAsync(name, cancellationToken);
        return Ok();
    }
}
