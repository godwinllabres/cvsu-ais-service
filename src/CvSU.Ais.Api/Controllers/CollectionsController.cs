using CvSU.Ais.Api.Auth;
using CvSU.Ais.Application.Collections;
using CvSU.Ais.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CvSU.Ais.Api.Controllers;

/// <summary>Collections (Official Receipts). GET/POST only. The create path is idempotency-keyed
/// so a receipt captured offline and replayed on reconnect is recorded exactly once.</summary>
[ApiController]
[Route("api/official-receipts")]
[Authorize(Policy = CollectionsPolicies.Record)]
public sealed class CollectionsController(CollectionsService collections) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ReceiptView>> Record(
        RecordReceiptRequest request, CancellationToken cancellationToken) =>
        Ok(await collections.RecordReceiptAsync(request, cancellationToken));

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ReceiptView>>> List(CancellationToken cancellationToken) =>
        Ok(await collections.ListAsync(cancellationToken));
}
