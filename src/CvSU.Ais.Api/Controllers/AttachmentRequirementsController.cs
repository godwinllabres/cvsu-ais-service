using CvSU.Ais.Application.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CvSU.Ais.Api.Controllers;

[ApiController]
[Route("api/attachment-requirements")]
[Authorize]
public sealed class AttachmentRequirementsController(AttachmentRequirementService service) : ControllerBase
{
    public sealed record CreateAttachmentRequirementRequest(
        string ReferenceDoctype,
        string? WorkflowState,
        string RequirementCode,
        string RequirementLabel,
        string ValidationMode,
        string? FilenameKeyword);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AttachmentRequirementView>>> List(
        [FromQuery] string? doctype,
        [FromQuery] string? state,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(doctype))
            return Ok(await service.ListForDoctypeAsync(doctype, state, cancellationToken));

        return Ok(await service.ListAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<AttachmentRequirementView>> Create(
        CreateAttachmentRequirementRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateAttachmentRequirementCommand(
            request.ReferenceDoctype,
            request.WorkflowState,
            request.RequirementCode,
            request.RequirementLabel,
            request.ValidationMode,
            request.FilenameKeyword);

        var view = await service.CreateAsync(command, cancellationToken);
        return CreatedAtAction(nameof(List), view);
    }
}
