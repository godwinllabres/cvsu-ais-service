using System.Security.Claims;
using CvSU.Ais.Application.Exports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CvSU.Ais.Api.Controllers;

[ApiController]
[Route("api/push-tokens")]
[Authorize]
public sealed class PushTokensController(PushTokenService service) : ControllerBase
{
    public sealed record RegisterTokenRequest(string Token, string Platform);

    /// <summary>Register a push notification token for the authenticated user.</summary>
    [HttpPost]
    public async Task<IActionResult> Register(RegisterTokenRequest request, CancellationToken cancellationToken)
    {
        var command = new RegisterPushTokenCommand(CurrentUser, request.Token, request.Platform);
        await service.RegisterAsync(command, cancellationToken);
        return NoContent();
    }

    /// <summary>List all push tokens for a given user.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PushTokenView>>> ListForUser(
        [FromQuery] string userId,
        CancellationToken cancellationToken) =>
        Ok(await service.ListForUserAsync(userId, cancellationToken));

    /// <summary>Deactivate (soft-delete) a push token by its ID.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        await service.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }

    private string CurrentUser => User.Identity?.Name ?? "anonymous";
}
