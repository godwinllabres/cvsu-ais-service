using System.Security.Claims;
using CvSU.Ais.Api.Auth;
using CvSU.Ais.Application.Exports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CvSU.Ais.Api.Controllers;

[ApiController]
[Route("api/findes-exports")]
[Authorize]
public sealed class FindesExportController(FindesExportService service) : ControllerBase
{
    public sealed record CreateFindesExportRequest(
        DateOnly ExportDate,
        IReadOnlyList<FindesExportLineDto> Lines,
        string? Remarks = null);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FindesExportView>>> List(CancellationToken cancellationToken) =>
        Ok(await service.ListAsync(cancellationToken));

    [HttpPost]
    [Authorize(Policy = ExportPolicies.Manage)]
    public async Task<ActionResult<FindesExportView>> Create(
        CreateFindesExportRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateFindesExportCommand(request.ExportDate, request.Lines, request.Remarks);
        var view = await service.CreateAsync(command, cancellationToken);
        return CreatedAtAction(nameof(Get), new { name = view.Name }, view);
    }

    [HttpGet("{name}")]
    public async Task<ActionResult<FindesExportDetailView>> Get(string name, CancellationToken cancellationToken) =>
        Ok(await service.GetAsync(name, cancellationToken));

    [HttpPost("{name}/approve")]
    [Authorize(Policy = ExportPolicies.Manage)]
    public async Task<ActionResult<FindesExportView>> Approve(string name, CancellationToken cancellationToken) =>
        Ok(await service.ApproveAsync(name, CurrentUser, cancellationToken));

    [HttpPost("{name}/export")]
    [Authorize(Policy = ExportPolicies.Manage)]
    public async Task<ActionResult<FindesExportView>> Export(string name, CancellationToken cancellationToken) =>
        Ok(await service.ExportAsync(name, CurrentUser, cancellationToken));

    [HttpPost("{name}/reject")]
    [Authorize(Policy = ExportPolicies.Manage)]
    public async Task<ActionResult<FindesExportView>> Reject(string name, CancellationToken cancellationToken) =>
        Ok(await service.RejectAsync(name, cancellationToken));

    private string CurrentUser => User.Identity?.Name ?? "anonymous";
}
