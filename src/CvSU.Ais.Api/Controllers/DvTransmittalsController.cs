using CvSU.Ais.Api.Auth;
using CvSU.Ais.Application.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CvSU.Ais.Api.Controllers;

[ApiController]
[Route("api/dv-transmittals")]
[Authorize]
public sealed class DvTransmittalsController(DvTransmittalService service) : ControllerBase
{
    public sealed record CreateDvTransmittalRequest(
        DateOnly TransmittalDate,
        string TransmittingOfficer,
        string ReceivingCashier,
        IReadOnlyList<DvTransmittalItemDto> Items,
        string? Remarks = null);

    public sealed record ReceiveDvTransmittalRequest(string ReceiverName, DateOnly ReceivedDate);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DvTransmittalView>>> List(CancellationToken ct) =>
        Ok(await service.ListAsync(ct));

    [HttpPost]
    [Authorize(Policy = PaymentPolicies.Transmittal)]
    public async Task<ActionResult<DvTransmittalView>> Create(CreateDvTransmittalRequest request, CancellationToken ct)
    {
        var cmd = new CreateDvTransmittalCommand(
            request.TransmittalDate,
            request.TransmittingOfficer,
            request.ReceivingCashier,
            request.Items,
            request.Remarks);

        var view = await service.CreateAsync(cmd, ct);
        return CreatedAtAction(nameof(Get), new { name = view.Name }, view);
    }

    [HttpGet("{name}")]
    public async Task<ActionResult<DvTransmittalDetailView>> Get(string name, CancellationToken ct) =>
        Ok(await service.GetAsync(name, ct));

    [HttpPost("{name}/transmit")]
    [Authorize(Policy = PaymentPolicies.Transmittal)]
    public async Task<ActionResult<DvTransmittalView>> Transmit(string name, CancellationToken ct) =>
        Ok(await service.TransmitAsync(name, ct));

    [HttpPost("{name}/receive")]
    [Authorize(Policy = PaymentPolicies.Transmittal)]
    public async Task<ActionResult<DvTransmittalView>> Receive(
        string name,
        ReceiveDvTransmittalRequest request,
        CancellationToken ct) =>
        Ok(await service.ReceiveAsync(name, request.ReceiverName, request.ReceivedDate, ct));

    [HttpPost("{name}/complete")]
    [Authorize(Policy = PaymentPolicies.Transmittal)]
    public async Task<ActionResult<DvTransmittalView>> Complete(string name, CancellationToken ct) =>
        Ok(await service.CompleteAsync(name, ct));
}
