using CvSU.Ais.Api.Auth;
using CvSU.Ais.Application.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CvSU.Ais.Api.Controllers;

[ApiController]
[Route("api/audit-intakes")]
[Authorize]
public sealed class AuditIntakesController(AuditIntakeService service) : ControllerBase
{
    public sealed record CreateAuditIntakeRequest(
        string DisbursementVoucherName,
        DateTime ReceivedTimestamp);

    public sealed record AuditRequest(string Result, string? Findings = null);

    public sealed record ReleaseRequest(string ReleasedTo);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AuditIntakeView>>> List(CancellationToken ct) =>
        Ok(await service.ListAsync(ct));

    [HttpPost]
    [Authorize(Policy = PaymentPolicies.AuditIntake)]
    public async Task<ActionResult<AuditIntakeView>> Create(CreateAuditIntakeRequest request, CancellationToken ct)
    {
        var cmd = new CreateAuditIntakeCommand(
            request.DisbursementVoucherName,
            request.ReceivedTimestamp);

        var view = await service.CreateAsync(cmd, ct);
        return CreatedAtAction(nameof(Get), new { name = view.Name }, view);
    }

    [HttpGet("{name}")]
    public async Task<ActionResult<AuditIntakeDetailView>> Get(string name, CancellationToken ct) =>
        Ok(await service.GetAsync(name, ct));

    [HttpPost("{name}/record")]
    [Authorize(Policy = PaymentPolicies.AuditIntake)]
    public async Task<ActionResult<AuditIntakeView>> Record(string name, CancellationToken ct) =>
        Ok(await service.RecordAsync(name, ct));

    [HttpPost("{name}/audit")]
    [Authorize(Policy = PaymentPolicies.AuditIntake)]
    public async Task<ActionResult<AuditIntakeView>> Audit(string name, AuditRequest request, CancellationToken ct) =>
        Ok(await service.AuditAsync(name, request.Result, request.Findings, ct));

    [HttpPost("{name}/release")]
    [Authorize(Policy = PaymentPolicies.AuditIntake)]
    public async Task<ActionResult<AuditIntakeView>> Release(string name, ReleaseRequest request, CancellationToken ct) =>
        Ok(await service.ReleaseAsync(name, request.ReleasedTo, ct));
}
