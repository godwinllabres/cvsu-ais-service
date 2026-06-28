using CvSU.Ais.Application.CashAdvances;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CvSU.Ais.Api.Controllers;

[ApiController]
[Route("api/liquidation-reports")]
[Authorize]
public sealed class LiquidationReportsController(LiquidationReportService service) : ControllerBase
{
    public sealed record CreateLiquidationReportRequest(
        string CashAdvanceName,
        DateOnly PostingDate,
        string? FundCluster,
        IReadOnlyList<LiquidationLineDto> Lines,
        string? Remarks = null);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LiquidationReportView>>> List(
        CancellationToken cancellationToken) =>
        Ok(await service.ListAsync(cancellationToken));

    [HttpPost]
    public async Task<ActionResult<LiquidationReportDetailView>> Create(
        CreateLiquidationReportRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateLiquidationReportCommand(
            CashAdvanceName: request.CashAdvanceName,
            PostingDate: request.PostingDate,
            FundCluster: request.FundCluster,
            Lines: request.Lines,
            Remarks: request.Remarks);

        var name = await service.CreateAsync(command, cancellationToken);
        var detail = await service.GetAsync(name, cancellationToken);
        return CreatedAtAction(nameof(Get), new { name }, detail);
    }

    [HttpGet("{name}")]
    public async Task<ActionResult<LiquidationReportDetailView>> Get(
        string name,
        CancellationToken cancellationToken) =>
        Ok(await service.GetAsync(name, cancellationToken));

    [HttpPost("{name}/submit")]
    public async Task<IActionResult> Submit(
        string name,
        CancellationToken cancellationToken)
    {
        await service.SubmitAsync(name, cancellationToken);
        return Ok();
    }

    [HttpPost("{name}/post")]
    public async Task<IActionResult> Post(
        string name,
        CancellationToken cancellationToken)
    {
        await service.PostAsync(name, cancellationToken);
        return Ok();
    }

    [HttpPost("{name}/cancel")]
    public async Task<IActionResult> Cancel(
        string name,
        CancellationToken cancellationToken)
    {
        await service.CancelAsync(name, cancellationToken);
        return Ok();
    }
}
