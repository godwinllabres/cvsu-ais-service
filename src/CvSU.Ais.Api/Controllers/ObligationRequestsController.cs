using CvSU.Ais.Api.Auth;
using CvSU.Ais.Application.Obligations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CvSU.Ais.Api.Controllers;

[ApiController]
[Route("api/obligation-requests")]
[Authorize]
public sealed class ObligationRequestsController(ObligationRequestService svc) : ControllerBase
{
    public sealed record CreateOrsRequest(
        string RequestingUnit,
        DateOnly PostingDate,
        int FiscalYear,
        string Purpose,
        decimal Amount,
        string FundingSourceCode,
        string? PapCode,
        string? LocationCode,
        string? ExpenseClass,
        string? RequestingOfficeUser,
        string? BudgetOfficerUser,
        IReadOnlyList<OrsLineItemDto> LineItems,
        string? Remarks);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrsView>>> List(CancellationToken ct) =>
        Ok(await svc.ListAsync(ct));

    [HttpGet("{name}")]
    public async Task<ActionResult<OrsDetailView>> Get(string name, CancellationToken ct)
    {
        try
        {
            return Ok(await svc.GetAsync(name, ct));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    [Authorize(Policy = ObligationPolicies.Create)]
    public async Task<ActionResult<OrsView>> Create(CreateOrsRequest request, CancellationToken ct)
    {
        var command = new CreateOrsCommand(
            request.RequestingUnit,
            request.PostingDate,
            request.FiscalYear,
            request.Purpose,
            request.Amount,
            request.FundingSourceCode,
            request.PapCode,
            request.LocationCode,
            request.ExpenseClass,
            request.RequestingOfficeUser,
            request.BudgetOfficerUser,
            request.LineItems,
            request.Remarks);

        var view = await svc.CreateAsync(command, ct);
        return CreatedAtAction(nameof(Get), new { name = view.Name }, view);
    }

    [HttpPost("{name}/submit")]
    [Authorize(Policy = ObligationPolicies.Submit)]
    public async Task<ActionResult<OrsView>> Submit(string name, CancellationToken ct)
    {
        try
        {
            return Ok(await svc.SubmitAsync(name, ct));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{name}/review")]
    [Authorize(Policy = ObligationPolicies.Review)]
    public async Task<ActionResult<OrsView>> Review(string name, CancellationToken ct)
    {
        try
        {
            return Ok(await svc.ReviewAsync(name, ct));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    public sealed record FundVerifyRequest(string AllotmentId);

    [HttpPost("{name}/fund-verify")]
    [Authorize(Policy = ObligationPolicies.FundVerify)]
    public async Task<ActionResult<OrsView>> FundVerify(
        string name, [FromBody] FundVerifyRequest body, CancellationToken ct)
    {
        try
        {
            return Ok(await svc.FundVerifyAsync(name, body.AllotmentId, ct));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{name}/sign")]
    [Authorize(Policy = ObligationPolicies.Sign)]
    public async Task<ActionResult<OrsView>> Sign(string name, CancellationToken ct)
    {
        try
        {
            return Ok(await svc.SignAsync(name, ct));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{name}/cancel")]
    [Authorize(Policy = ObligationPolicies.Cancel)]
    public async Task<ActionResult<OrsView>> Cancel(string name, CancellationToken ct)
    {
        try
        {
            return Ok(await svc.CancelAsync(name, ct));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
