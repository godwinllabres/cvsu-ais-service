using CvSU.Ais.Api.Auth;
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
        decimal TotalGrossPay,
        decimal TotalTaxWithheld,
        decimal TotalGsis,
        decimal TotalPagibig,
        decimal TotalPhilhealth,
        int TotalRecords,
        IReadOnlyList<PayrollLoanDeductionDto>? LoanDeductions,
        string? Remarks);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PayrollEntryView>>> List(CancellationToken cancellationToken) =>
        Ok(await service.ListAsync(cancellationToken));

    [HttpPost]
    [Authorize(Policy = PayrollPolicies.Manage)]
    public async Task<ActionResult<PayrollEntryView>> Create(
        CreatePayrollEntryRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreatePayrollEntryCommand(
            request.PayrollType,
            request.PayrollPeriod,
            request.PostingDate,
            request.FundCluster,
            request.TotalGrossPay,
            request.TotalTaxWithheld,
            request.TotalGsis,
            request.TotalPagibig,
            request.TotalPhilhealth,
            request.TotalRecords,
            request.LoanDeductions ?? [],
            request.Remarks);

        var view = await service.CreateAsync(command, cancellationToken);
        return CreatedAtAction(nameof(Get), new { name = view.Name }, view);
    }

    [HttpGet("{name}")]
    public async Task<ActionResult<PayrollEntryDetailView>> Get(string name, CancellationToken cancellationToken) =>
        Ok(await service.GetAsync(name, cancellationToken));

    [HttpPost("{name}/validate")]
    [Authorize(Policy = PayrollPolicies.Manage)]
    public async Task<IActionResult> Validate(string name, CancellationToken cancellationToken)
    {
        await service.ValidateAsync(name, cancellationToken);
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
