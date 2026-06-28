using CvSU.Ais.Application.Payroll;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CvSU.Ais.Api.Controllers;

[ApiController]
[Route("api/salary-tranches")]
[Authorize]
public sealed class SalaryTranchesController(SalaryTrancheService service) : ControllerBase
{
    public sealed record CreateSalaryTrancheRequest(
        string SslLaw,
        int TrancheNumber,
        int EffectiveYear,
        DateOnly? EffectiveDate,
        string? DbmCircularReference,
        IReadOnlyList<SalaryTrancheEntryDto>? Entries,
        string? Remarks);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SalaryTrancheView>>> List(CancellationToken cancellationToken) =>
        Ok(await service.ListAsync(cancellationToken));

    [HttpPost]
    public async Task<ActionResult<SalaryTrancheView>> Create(
        CreateSalaryTrancheRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateSalaryTrancheCommand(
            request.SslLaw,
            request.TrancheNumber,
            request.EffectiveYear,
            request.EffectiveDate,
            request.DbmCircularReference,
            request.Entries,
            request.Remarks);

        var view = await service.CreateAsync(command, cancellationToken);
        return CreatedAtAction(nameof(Get), new { name = view.Name }, view);
    }

    [HttpGet("{name}")]
    public async Task<ActionResult<SalaryTrancheDetailView>> Get(string name, CancellationToken cancellationToken) =>
        Ok(await service.GetAsync(name, cancellationToken));

    [HttpPost("{name}/activate")]
    public async Task<IActionResult> Activate(string name, CancellationToken cancellationToken)
    {
        await service.ActivateAsync(name, cancellationToken);
        return Ok();
    }

    [HttpPost("{name}/supersede")]
    public async Task<IActionResult> Supersede(string name, CancellationToken cancellationToken)
    {
        await service.SupersedeAsync(name, cancellationToken);
        return Ok();
    }
}
