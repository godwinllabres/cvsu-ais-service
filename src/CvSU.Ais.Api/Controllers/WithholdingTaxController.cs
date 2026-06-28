using CvSU.Ais.Application.Compliance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CvSU.Ais.Api.Controllers;

/// <summary>
/// Withholding Tax Statement management. Statements capture the breakdown of
/// EWT liabilities (by ATC code / tax class) for a given tax period, and follow
/// a Draft → ForReview → Approved/Rejected workflow before GL posting.
/// </summary>
[ApiController]
[Route("api/withholding-tax")]
[Authorize]
public sealed class WithholdingTaxController(WhtStatementService service) : ControllerBase
{
    private string CurrentUser =>
        User.Identity?.Name ?? "anonymous";

    /// <summary>List all Withholding Tax Statements (thin view).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WhtStatementView>>> List(CancellationToken cancellationToken) =>
        Ok(await service.ListAsync(cancellationToken));

    /// <summary>Create a new Withholding Tax Statement in Draft status.</summary>
    [HttpPost]
    public async Task<ActionResult<WhtStatementDetailView>> Create(
        [FromBody] CreateWhtStatementCommand command,
        CancellationToken cancellationToken)
    {
        var name = $"WHT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
        try
        {
            var detail = await service.CreateAsync(command, name, cancellationToken);
            return CreatedAtAction(nameof(Get), new { name = detail.Name }, detail);
        }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
    }

    /// <summary>Get full detail of a Withholding Tax Statement including all lines.</summary>
    [HttpGet("{name}")]
    public async Task<ActionResult<WhtStatementDetailView>> Get(string name, CancellationToken cancellationToken)
    {
        try { return Ok(await service.GetAsync(name, cancellationToken)); }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
    }

    /// <summary>Approve the statement. Reviewer is the authenticated caller.</summary>
    [HttpPost("{name}/approve")]
    public async Task<ActionResult<WhtStatementDetailView>> Approve(string name, CancellationToken cancellationToken)
    {
        try { return Ok(await service.ApproveAsync(name, CurrentUser, cancellationToken)); }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
    }

    /// <summary>Reject the statement. Reviewer is the authenticated caller.</summary>
    [HttpPost("{name}/reject")]
    public async Task<ActionResult<WhtStatementDetailView>> Reject(string name, CancellationToken cancellationToken)
    {
        try { return Ok(await service.RejectAsync(name, CurrentUser, cancellationToken)); }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
    }
}
