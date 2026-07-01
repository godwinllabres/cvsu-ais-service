using CvSU.Ais.Api.Auth;
using CvSU.Ais.Application.Exports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CvSU.Ais.Api.Controllers;

[ApiController]
[Route("api/bank-collection-reports")]
[Authorize]
public sealed class BankCollectionReportController(BankCollectionReportService service) : ControllerBase
{
    public sealed record CreateBankCollectionReportRequest(
        DateOnly ReportDate,
        IReadOnlyList<BankCollectionLineDto> Lines,
        string? Remarks = null);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BankCollectionReportView>>> List(CancellationToken cancellationToken) =>
        Ok(await service.ListAsync(cancellationToken));

    [HttpPost]
    [Authorize(Policy = ExportPolicies.Manage)]
    public async Task<ActionResult<BankCollectionReportView>> Create(
        CreateBankCollectionReportRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateBankCollectionReportCommand(request.ReportDate, request.Lines, request.Remarks);
        var view = await service.CreateAsync(command, cancellationToken);
        return CreatedAtAction(nameof(Get), new { name = view.Name }, view);
    }

    [HttpGet("{name}")]
    public async Task<ActionResult<BankCollectionReportDetailView>> Get(
        string name,
        CancellationToken cancellationToken) =>
        Ok(await service.GetAsync(name, cancellationToken));

    [HttpPost("{name}/reconcile")]
    [Authorize(Policy = ExportPolicies.Manage)]
    public async Task<ActionResult<BankCollectionReportView>> Reconcile(
        string name,
        CancellationToken cancellationToken) =>
        Ok(await service.ReconcileAsync(name, cancellationToken));
}
