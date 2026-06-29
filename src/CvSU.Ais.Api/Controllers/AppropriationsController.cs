using CvSU.Ais.Api.Auth;
using CvSU.Ais.Application.Budget;
using CvSU.Ais.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CvSU.Ais.Api.Controllers;

[ApiController]
[Route("api/appropriations")]
[Authorize(Policy = BudgetPolicies.Manage)]
public sealed class AppropriationsController(BudgetExecutionService budget) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<AppropriationView>> Create(
        AppropriationCreateRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateAppropriationCommand(
            request.FiscalYear, request.FundingSourceCode, request.PapCode, request.LocationCode,
            request.ExpenseClass, request.ObjectAccountCode, request.FinalAppropriation);

        var view = await budget.CreateAppropriationAsync(command, cancellationToken);
        return Ok(view);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AppropriationView>>> List(CancellationToken cancellationToken) =>
        Ok(await budget.ListAppropriationsAsync(cancellationToken));

    [HttpPost("{appropriationId}/allotments")]
    public async Task<ActionResult<AllotmentView>> Allot(
        string appropriationId, AmountRequest request, CancellationToken cancellationToken)
    {
        var view = await budget.AllotAsync(appropriationId, request.Amount, cancellationToken);
        return Ok(view);
    }
}
