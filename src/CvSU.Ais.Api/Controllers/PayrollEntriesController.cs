using CvSU.Ais.Application.Payroll;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CvSU.Ais.Api.Controllers;

[ApiController]
[Route("api/payroll-entries")]
[Authorize]
public sealed class PayrollEntriesController(PayrollEntryService service) : ControllerBase
{
    public sealed record CreatePayrollEntryRequest(
        string PayrollType,
        string PayrollPeriod,
        DateOnly PostingDate,
        string? FundCluster,
        IReadOnlyList<PayrollLoanDeductionDto>? LoanDeductions,
        string? Remarks);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PayrollEntryView>>> List(CancellationToken cancellationToken) =>
        Ok(await service.ListAsync(cancellationToken));

    [HttpPost]
    public async Task<ActionResult<PayrollEntryView>> Create(
        CreatePayrollEntryRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreatePayrollEntryCommand(
            request.PayrollType,
            request.PayrollPeriod,
            request.PostingDate,
            request.FundCluster,
            request.LoanDeductions ?? [],
            request.Remarks);

        var view = await service.CreateAsync(command, cancellationToken);
        return CreatedAtAction(nameof(Get), new { name = view.Name }, view);
    }

    [HttpGet("{name}")]
    public async Task<ActionResult<PayrollEntryDetailView>> Get(string name, CancellationToken cancellationToken) =>
        Ok(await service.GetAsync(name, cancellationToken));

    [HttpPost("{name}/validate")]
    public async Task<IActionResult> Validate(string name, CancellationToken cancellationToken)
    {
        await service.ValidateAsync(name, cancellationToken);
        return Ok();
    }

    [HttpPost("{name}/post")]
    public async Task<IActionResult> Post(string name, CancellationToken cancellationToken)
    {
        await service.PostAsync(name, cancellationToken);
        return Ok();
    }

    [HttpPost("{name}/cancel")]
    public async Task<IActionResult> Cancel(string name, CancellationToken cancellationToken)
    {
        await service.CancelAsync(name, cancellationToken);
        return Ok();
    }
}
