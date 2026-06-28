using CvSU.Ais.Application.Compliance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CvSU.Ais.Api.Controllers;

/// <summary>
/// BIR Form 2307 (Certificate of Creditable Withholding Tax at Source) management.
/// A certificate is created in Draft, sent for review, then Approved or Rejected.
/// The reviewer identity is resolved from the authenticated user principal.
/// </summary>
[ApiController]
[Route("api/bir-2307")]
[Authorize]
public sealed class Bir2307Controller(Bir2307Service service) : ControllerBase
{
    private string CurrentUser =>
        User.Identity?.Name ?? "anonymous";

    /// <summary>List all BIR 2307 certificates (thin view).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Bir2307View>>> List(CancellationToken cancellationToken) =>
        Ok(await service.ListAsync(cancellationToken));

    /// <summary>Create a new BIR 2307 certificate in Draft status.</summary>
    [HttpPost]
    public async Task<ActionResult<Bir2307DetailView>> Create(
        [FromBody] CreateBir2307Command command,
        CancellationToken cancellationToken)
    {
        var name = $"BIR2307-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
        var detail = await service.CreateAsync(command, name, cancellationToken);
        return CreatedAtAction(nameof(Get), new { name = detail.Name }, detail);
    }

    /// <summary>Get full detail of a single BIR 2307 certificate.</summary>
    [HttpGet("{name}")]
    public async Task<ActionResult<Bir2307DetailView>> Get(string name, CancellationToken cancellationToken)
    {
        try { return Ok(await service.GetAsync(name, cancellationToken)); }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
    }

    /// <summary>Approve the BIR 2307 certificate. Reviewer is the authenticated caller.</summary>
    [HttpPost("{name}/approve")]
    public async Task<ActionResult<Bir2307DetailView>> Approve(string name, CancellationToken cancellationToken)
    {
        try { return Ok(await service.ApproveAsync(name, CurrentUser, cancellationToken)); }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
    }

    /// <summary>Reject the BIR 2307 certificate. Reviewer is the authenticated caller.</summary>
    [HttpPost("{name}/reject")]
    public async Task<ActionResult<Bir2307DetailView>> Reject(string name, CancellationToken cancellationToken)
    {
        try { return Ok(await service.RejectAsync(name, CurrentUser, cancellationToken)); }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
    }
}
