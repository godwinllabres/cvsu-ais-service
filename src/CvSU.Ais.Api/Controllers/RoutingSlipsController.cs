using CvSU.Ais.Application.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CvSU.Ais.Api.Controllers;

[ApiController]
[Route("api/routing-slips")]
[Authorize]
public sealed class RoutingSlipsController(RoutingSlipService service) : ControllerBase
{
    public sealed record CreateRoutingSlipRequest(
        string RoutingTemplateName,
        string? ReferenceDoctype,
        string? ReferenceName);

    public sealed record AdvanceSlipRequest(string HandledBy, string? Remarks);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RoutingSlipView>>> List(CancellationToken cancellationToken) =>
        Ok(await service.ListAsync(cancellationToken));

    [HttpPost]
    public async Task<ActionResult<RoutingSlipDetailView>> Create(
        CreateRoutingSlipRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateRoutingSlipCommand(
            request.RoutingTemplateName,
            request.ReferenceDoctype,
            request.ReferenceName);

        var view = await service.CreateAsync(command, cancellationToken);
        return CreatedAtAction(nameof(Get), new { name = view.Name }, view);
    }

    [HttpGet("{name}")]
    public async Task<ActionResult<RoutingSlipDetailView>> Get(string name, CancellationToken cancellationToken) =>
        Ok(await service.GetAsync(name, cancellationToken));

    [HttpPost("{name}/advance")]
    public async Task<ActionResult<RoutingSlipDetailView>> Advance(
        string name, AdvanceSlipRequest request, CancellationToken cancellationToken) =>
        Ok(await service.AdvanceAsync(name, request.HandledBy, request.Remarks, cancellationToken));

    [HttpPost("{name}/cancel")]
    public async Task<ActionResult<RoutingSlipDetailView>> Cancel(string name, CancellationToken cancellationToken) =>
        Ok(await service.CancelAsync(name, cancellationToken));
}
