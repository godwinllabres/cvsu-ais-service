using System.Security.Claims;
using CvSU.Ais.Api.Auth;
using CvSU.Ais.Application.DisbursementVouchers;
using CvSU.Ais.Domain.Disbursement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CvSU.Ais.Api.Controllers;

[ApiController]
[Route("api/disbursement-vouchers")]
[Authorize]
public sealed class DisbursementVouchersController(DisbursementVoucherService service) : ControllerBase
{
    public sealed record CreateDvRequest(
        int FiscalYear,
        decimal Amount,
        string FundingSourceCode,
        bool BudgetCertified = false,
        bool InternalAuditConfirmed = false,
        bool EndUserConfirmed = false,
        bool AccountantSigned = false);

    [HttpPost]
    [Authorize(Policy = DvPolicies.Create)]
    public async Task<ActionResult<DvStateView>> Create(CreateDvRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateDvCommand(
            CurrentUser, request.FiscalYear, request.Amount, request.FundingSourceCode,
            request.BudgetCertified, request.InternalAuditConfirmed,
            request.EndUserConfirmed, request.AccountantSigned);

        var view = await service.CreateAsync(command, cancellationToken);
        return CreatedAtAction(nameof(Get), new { name = view.Name }, view);
    }

    [HttpGet("{name}")]
    public async Task<ActionResult<DvStateView>> Get(string name, CancellationToken cancellationToken) =>
        Ok(await service.GetAsync(name, cancellationToken));

    [HttpPost("{name}/request-ia-audit")]
    [Authorize(Policy = DvPolicies.RequestIaAudit)]
    public Task<ActionResult<DvStateView>> RequestIaAudit(string name, CancellationToken ct) =>
        Transition(name, DvAction.RequestIaAudit, ct);

    [HttpPost("{name}/submit")]
    [Authorize(Policy = DvPolicies.Submit)]
    public Task<ActionResult<DvStateView>> Submit(string name, CancellationToken ct) =>
        Transition(name, DvAction.Submit, ct);

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

    [HttpPost("{name}/reject")]
    [Authorize(Policy = DvPolicies.Reject)]
    public Task<ActionResult<DvStateView>> Reject(string name, CancellationToken ct) =>
        Transition(name, DvAction.Reject, ct);

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
