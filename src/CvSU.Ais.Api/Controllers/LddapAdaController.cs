using CvSU.Ais.Api.Auth;
using CvSU.Ais.Application.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CvSU.Ais.Api.Controllers;

[ApiController]
[Route("api/lddap-adas")]
[Authorize]
public sealed class LddapAdaController(LddapAdaService service) : ControllerBase
{
    public sealed record CreateLddapAdaRequest(
        DateOnly PeriodFrom,
        DateOnly PeriodTo,
        string? FundCluster,
        string BankName,
        string BankAccountNumber,
        IReadOnlyList<LddapAdaItemDto> Items,
        string? Remarks = null);

    public sealed record TransmitLddapAdaRequest(DateOnly TransmittedDate);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LddapAdaView>>> List(CancellationToken ct) =>
        Ok(await service.ListAsync(ct));

    [HttpPost]
    [Authorize(Policy = PaymentPolicies.LddapManage)]
    public async Task<ActionResult<LddapAdaView>> Create(CreateLddapAdaRequest request, CancellationToken ct)
    {
        var cmd = new CreateLddapAdaCommand(
            request.PeriodFrom,
            request.PeriodTo,
            request.FundCluster,
            request.BankName,
            request.BankAccountNumber,
            request.Items,
            request.Remarks);

        var view = await service.CreateAsync(cmd, ct);
        return CreatedAtAction(nameof(Get), new { name = view.Name }, view);
    }

    [HttpGet("{name}")]
    public async Task<ActionResult<LddapAdaDetailView>> Get(string name, CancellationToken ct) =>
        Ok(await service.GetAsync(name, ct));

    [HttpPost("{name}/approve")]
    [Authorize(Policy = PaymentPolicies.LddapApprove)]
    public async Task<ActionResult<LddapAdaView>> Approve(string name, CancellationToken ct) =>
        Ok(await service.ApproveAsync(name, ct));

    [HttpPost("{name}/transmit")]
    [Authorize(Policy = PaymentPolicies.LddapTransmit)]
    public async Task<ActionResult<LddapAdaView>> Transmit(string name, TransmitLddapAdaRequest request, CancellationToken ct) =>
        Ok(await service.TransmitAsync(name, request.TransmittedDate, ct));

    [HttpPost("{name}/cancel")]
    [Authorize(Policy = PaymentPolicies.LddapManage)]
    public async Task<ActionResult<LddapAdaView>> Cancel(string name, CancellationToken ct) =>
        Ok(await service.CancelAsync(name, ct));
}
