using CvSU.Ais.Application.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CvSU.Ais.Api.Controllers;

[ApiController]
[Route("api/routing-templates")]
[Authorize]
public sealed class RoutingTemplatesController(RoutingTemplateService service) : ControllerBase
{
    public sealed record CreateRoutingTemplateRequest(
        string Name,
        string TemplateName,
        string? DocumentType,
        string? Description,
        decimal? MinAmount,
        decimal? MaxAmount,
        IReadOnlyList<RoutingTemplateStepDto> Steps);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RoutingTemplateView>>> List(CancellationToken cancellationToken) =>
        Ok(await service.ListAsync(cancellationToken));

    [HttpPost]
    public async Task<ActionResult<RoutingTemplateView>> Create(
        CreateRoutingTemplateRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateRoutingTemplateCommand(
            request.Name,
            request.TemplateName,
            request.DocumentType,
            request.Description,
            request.MinAmount,
            request.MaxAmount,
            request.Steps);

        var view = await service.CreateAsync(command, cancellationToken);
        return CreatedAtAction(nameof(Get), new { name = view.Name }, view);
    }

    [HttpGet("{name}")]
    public async Task<ActionResult<RoutingTemplateView>> Get(string name, CancellationToken cancellationToken) =>
        Ok(await service.GetAsync(name, cancellationToken));
}
