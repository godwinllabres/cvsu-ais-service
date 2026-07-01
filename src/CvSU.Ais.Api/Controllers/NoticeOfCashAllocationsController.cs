using CvSU.Ais.Api.Auth;
using CvSU.Ais.Application.Obligations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CvSU.Ais.Api.Controllers;

[ApiController]
[Route("api/notices-of-cash-allocation")]
[Authorize]
public sealed class NoticeOfCashAllocationsController(NcaService svc) : ControllerBase
{
    public sealed record CreateNcaRequest(
        string NcaNumber,
        DateOnly DateReceived,
        int FiscalYear,
        string FundingSourceCode,
        DateOnly ValidityDate,
        decimal NcaAmount,
        string? Remarks);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NcaView>>> List(CancellationToken ct) =>
        Ok(await svc.ListAsync(ct));

    [HttpGet("{ncaNumber}")]
    public async Task<ActionResult<NcaView>> Get(string ncaNumber, CancellationToken ct)
    {
        try
        {
            return Ok(await svc.GetAsync(ncaNumber, ct));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    [Authorize(Policy = ObligationPolicies.ManageNca)]
    public async Task<ActionResult<NcaView>> Create(CreateNcaRequest request, CancellationToken ct)
    {
        var command = new CreateNcaCommand(
            request.NcaNumber,
            request.DateReceived,
            request.FiscalYear,
            request.FundingSourceCode,
            request.ValidityDate,
            request.NcaAmount,
            request.Remarks);

        var view = await svc.AddAsync(command, ct);
        return CreatedAtAction(nameof(Get), new { ncaNumber = view.NcaNumber }, view);
    }
}
