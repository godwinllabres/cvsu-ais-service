using System.Security.Claims;
using CvSU.Ais.Api.Auth;
using CvSU.Ais.Application.DisbursementVouchers;
using CvSU.Ais.Contracts;
using CvSU.Ais.Domain.Disbursement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CvSU.Ais.Api.Controllers;

[ApiController]
[Route("api/disbursement-vouchers")]
[Authorize]
public sealed class DisbursementVouchersController(DisbursementVoucherService service) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = DvPolicies.Create)]
    public async Task<ActionResult<DvStateView>> Create(DvCreateRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateDvCommand(
            Encoder: CurrentUser,
            FiscalYear: request.FiscalYear,
            Amount: request.Amount,
            FundingSourceCode: request.FundingSourceCode,
            PapCode: request.PapCode,
            LocationCode: request.LocationCode,
            ExpenseClass: request.ExpenseClass,
            ObjectAccountCode: request.ObjectAccountCode,
            TaxWithheld: request.TaxWithheld,
            DvType: request.DvType);

        var view = await service.CreateAsync(command, cancellationToken);
        return CreatedAtAction(nameof(Get), new { name = view.Name }, view);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DvStateView>>> List(CancellationToken cancellationToken) =>
        Ok(await service.ListAsync(cancellationToken));

    [HttpGet("{name}")]
    public async Task<ActionResult<DvDetailView>> Get(string name, CancellationToken cancellationToken) =>
        Ok(await service.GetAsync(name, cancellationToken));

    /// <summary>Re-encode a Draft DV. Rejected by the aggregate once it has left Draft.</summary>
    [HttpPut("{name}")]
    [Authorize(Policy = DvPolicies.Edit)]
    public async Task<ActionResult<DvDetailView>> Update(
        string name, DvCreateRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateDvCommand(
            Amount: request.Amount,
            FundingSourceCode: request.FundingSourceCode,
            DvType: request.DvType,
            PapCode: request.PapCode,
            LocationCode: request.LocationCode,
            ExpenseClass: request.ExpenseClass,
            ObjectAccountCode: request.ObjectAccountCode,
            TaxWithheld: request.TaxWithheld);

        return Ok(await service.UpdateAsync(name, command, cancellationToken));
    }

    /// <summary>POST sibling of <see cref="Update"/> for clients restricted to GET/POST
    /// (e.g. the MAUI desktop). Identical effect, same Draft-only guard and edit policy.</summary>
    [HttpPost("{name}/edit")]
    [Authorize(Policy = DvPolicies.Edit)]
    public Task<ActionResult<DvDetailView>> UpdateViaPost(
        string name, DvCreateRequest request, CancellationToken cancellationToken) =>
        Update(name, request, cancellationToken);

    [HttpPost("{name}/request-ia-audit")]
    [Authorize(Policy = DvPolicies.RequestIaAudit)]
    public Task<ActionResult<DvStateView>> RequestIaAudit(string name, CancellationToken ct) =>
        Transition(name, DvAction.RequestIaAudit, ct);

    [HttpPost("{name}/submit")]
    [Authorize(Policy = DvPolicies.Submit)]
    public Task<ActionResult<DvStateView>> Submit(string name, CancellationToken ct) =>
        Transition(name, DvAction.Submit, ct);

    [HttpPost("{name}/return-to-clerk")]
    [Authorize(Policy = DvPolicies.ReturnToClerk)]
    public Task<ActionResult<DvStateView>> ReturnToClerk(string name, CancellationToken ct) =>
        Transition(name, DvAction.ReturnToClerk, ct);

    [HttpPost("{name}/approve")]
    [Authorize(Policy = DvPolicies.Approve)]
    public Task<ActionResult<DvStateView>> Approve(string name, CancellationToken ct) =>
        Transition(name, DvAction.Approve, ct);

    [HttpPost("{name}/approve-for-payment")]
    [Authorize(Policy = DvPolicies.ApproveForPayment)]
    public Task<ActionResult<DvStateView>> ApproveForPayment(string name, CancellationToken ct) =>
        Transition(name, DvAction.ApproveForPayment, ct);

    [HttpPost("{name}/post")]
    [Authorize(Policy = DvPolicies.Post)]
    public Task<ActionResult<DvStateView>> Post(string name, CancellationToken ct) =>
        Transition(name, DvAction.Post, ct);

    [HttpPost("{name}/release")]
    [Authorize(Policy = DvPolicies.Release)]
    public Task<ActionResult<DvStateView>> Release(string name, CancellationToken ct) =>
        Transition(name, DvAction.Release, ct);

    [HttpPost("{name}/close")]
    [Authorize(Policy = DvPolicies.Close)]
    public Task<ActionResult<DvStateView>> Close(string name, CancellationToken ct) =>
        Transition(name, DvAction.Close, ct);

    /// <summary>Records the payment instrument (cheque/ADA/transfer reference). The
    /// reference must be unique across all DVs — the duplicate-disbursement guard.</summary>
    [HttpPost("{name}/payment")]
    [Authorize(Policy = DvPolicies.RecordPayment)]
    public async Task<ActionResult<DvStateView>> RecordPayment(
        string name, DvPaymentRequest request, CancellationToken ct) =>
        Ok(await service.RecordPaymentAsync(name, request.Method, request.Reference, ct));

    /// <summary>Records a certification, asserted by the officer responsible for it. The
    /// required role varies per certification, so the aggregate enforces it (403 on a
    /// caller who lacks the role) rather than a single static endpoint policy.</summary>
    [HttpPost("{name}/certify")]
    public async Task<ActionResult<DvDetailView>> Certify(
        string name, DvCertifyRequest request, CancellationToken ct)
    {
        var context = new TransitionContext(CurrentUser, CurrentRoles);
        return Ok(await service.CertifyAsync(name, request.Certification, context, ct));
    }

    private async Task<ActionResult<DvStateView>> Transition(string name, DvAction action, CancellationToken ct)
    {
        var context = new TransitionContext(CurrentUser, CurrentRoles);
        var view = await service.FireAsync(name, action, context, ct);
        return Ok(view);
    }

    private string CurrentUser => User.Identity?.Name ?? "anonymous";

    private string[] CurrentRoles =>
        User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray();
}
