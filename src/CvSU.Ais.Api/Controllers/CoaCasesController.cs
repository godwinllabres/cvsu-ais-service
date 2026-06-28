using CvSU.Ais.Application.Compliance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CvSU.Ais.Api.Controllers;

/// <summary>
/// COA audit-case lifecycle management. A case is opened when the agency
/// receives a Notice of Disallowance / Notice of Charge (ND/NC) from COA
/// and progresses through recording, NFD, COE, settlement and final submission.
/// </summary>
[ApiController]
[Route("api/coa-cases")]
[Authorize]
public sealed class CoaCasesController(CoaCaseService service) : ControllerBase
{
    /// <summary>List all COA cases (thin view).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CoaCaseView>>> List(CancellationToken cancellationToken) =>
        Ok(await service.ListAsync(cancellationToken));

    /// <summary>Open a new COA case on receipt of an ND/NC from COA.</summary>
    [HttpPost]
    public async Task<ActionResult<CoaCaseDetailView>> Create(
        [FromBody] CreateCoaCaseCommand command,
        CancellationToken cancellationToken)
    {
        var name = $"COA-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
        var detail = await service.CreateAsync(command, name, cancellationToken);
        return CreatedAtAction(nameof(Get), new { name = detail.Name }, detail);
    }

    /// <summary>Get full detail of a single COA case.</summary>
    [HttpGet("{name}")]
    public async Task<ActionResult<CoaCaseDetailView>> Get(string name, CancellationToken cancellationToken)
    {
        try { return Ok(await service.GetAsync(name, cancellationToken)); }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
    }

    /// <summary>Mark the case as Recorded in the internal register.</summary>
    [HttpPost("{name}/record")]
    public async Task<IActionResult> Record(string name, CancellationToken cancellationToken)
    {
        try { await service.RecordAsync(name, cancellationToken); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
    }

    /// <summary>Record receipt of the Notice of Final Decision (NFD).</summary>
    [HttpPost("{name}/issue-nfd")]
    public async Task<IActionResult> IssueNfd(string name, CancellationToken cancellationToken)
    {
        try { await service.IssueNfdAsync(name, cancellationToken); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
    }

    /// <summary>Issue the Certificate of Exemption (COE) to the liable party.</summary>
    [HttpPost("{name}/issue-coe")]
    public async Task<IActionResult> IssueCoe(string name, CancellationToken cancellationToken)
    {
        try { await service.IssueCoeAsync(name, cancellationToken); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
    }

    /// <summary>Mark the case as Settled after payment or payroll deduction.</summary>
    [HttpPost("{name}/settle")]
    public async Task<IActionResult> Settle(string name, CancellationToken cancellationToken)
    {
        try { await service.SettleAsync(name, cancellationToken); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
    }

    /// <summary>Submit the settled case back to COA.</summary>
    [HttpPost("{name}/submit")]
    public async Task<IActionResult> Submit(string name, CancellationToken cancellationToken)
    {
        try { await service.SubmitAsync(name, cancellationToken); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
    }
}
