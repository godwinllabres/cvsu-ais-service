using CvSU.Ais.Api.Auth;
using CvSU.Ais.Application.Budget;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CvSU.Ais.Api.Controllers;

[ApiController]
[Route("api/allotments")]
[Authorize(Policy = BudgetPolicies.Manage)]
public sealed class AllotmentsController(BudgetExecutionService budget) : ControllerBase
{
    public sealed record ObligateRequest(decimal Amount);

    /// <summary>Record an obligation (ORS/BURS) against the allotment — budget
    /// registry only, never the accrual GL.</summary>
    [HttpPost("{allotmentId}/obligations")]
    public async Task<ActionResult<ObligationView>> Obligate(
        string allotmentId, ObligateRequest request, CancellationToken cancellationToken)
    {
        var view = await budget.ObligateAsync(allotmentId, request.Amount, cancellationToken);
        return Ok(view);
    }
}
