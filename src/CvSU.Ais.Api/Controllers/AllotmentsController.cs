using CvSU.Ais.Api.Auth;
using CvSU.Ais.Application.Budget;
using CvSU.Ais.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CvSU.Ais.Api.Controllers;

[ApiController]
[Route("api/allotments")]
[Authorize(Policy = BudgetPolicies.Manage)]
public sealed class AllotmentsController(BudgetExecutionService budget) : ControllerBase
{
    /// <summary>Record an obligation (ORS/BURS) against the allotment — budget
    /// registry only, never the accrual GL.</summary>
    [HttpPost("{allotmentId}/obligations")]
    public async Task<ActionResult<ObligationView>> Obligate(
        string allotmentId, AmountRequest request, CancellationToken cancellationToken)
    {
        var view = await budget.ObligateAsync(allotmentId, request.Amount, cancellationToken);
        return Ok(view);
    }
}
