using CvSU.Ais.Api.Auth;
using CvSU.Ais.Application.JournalEntries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CvSU.Ais.Api.Controllers;

[ApiController]
[Route("api/journal-entries")]
[Authorize]
public sealed class JournalEntriesController(JournalEntryService svc) : ControllerBase
{
    private string CurrentUser => User.Identity?.Name ?? "anonymous";

    /// <summary>Returns all journal entries ordered by posting date descending.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<JournalEntryView>>> List(CancellationToken ct) =>
        Ok(await svc.ListAsync(ct));

    /// <summary>Returns the full detail of a single journal entry including its lines.</summary>
    [HttpGet("{name}")]
    public async Task<ActionResult<JournalEntryDetailView>> Get(string name, CancellationToken ct)
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

    /// <summary>Creates a new journal entry in Draft status. Returns 201 with the created view.</summary>
    [HttpPost]
    [Authorize(Policy = JePolicies.Create)]
    public async Task<ActionResult<JournalEntryView>> Create(
        [FromBody] CreateJournalEntryCommand command,
        CancellationToken ct)
    {
        var view = await svc.CreateAsync(command, ct);
        return CreatedAtAction(nameof(Get), new { name = view.Name }, view);
    }

    /// <summary>Approves the journal entry. The current authenticated user is recorded as approver.</summary>
    [HttpPost("{name}/approve")]
    [Authorize(Policy = JePolicies.Approve)]
    public async Task<IActionResult> Approve(string name, CancellationToken ct)
    {
        try
        {
            await svc.ApproveAsync(name, CurrentUser, ct);
            return Ok();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>Marks the journal entry as Posted (committed to the GL).</summary>
    [HttpPost("{name}/post")]
    [Authorize(Policy = JePolicies.Post)]
    public async Task<IActionResult> Post(string name, CancellationToken ct)
    {
        try
        {
            await svc.PostAsync(name, ct);
            return Ok();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>Cancels (rejects) the journal entry.</summary>
    [HttpPost("{name}/cancel")]
    [Authorize(Policy = JePolicies.Cancel)]
    public async Task<IActionResult> Cancel(string name, CancellationToken ct)
    {
        try
        {
            await svc.CancelAsync(name, ct);
            return Ok();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
