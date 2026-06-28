using CvSU.Ais.Application.ReferenceData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CvSU.Ais.Api.Controllers;

// ── PAP Codes ─────────────────────────────────────────────────────────────────

[ApiController]
[Route("api/pap-codes")]
[Authorize]
public sealed class PapCodesController(PapCodeService svc) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PapCodeView>>> List(CancellationToken cancellationToken) =>
        Ok(await svc.ListAsync(cancellationToken));

    [HttpGet("{code}")]
    public async Task<ActionResult<PapCodeView>> Get(string code, CancellationToken cancellationToken)
    {
        var view = await svc.GetAsync(code, cancellationToken);
        return view is null ? NotFound() : Ok(view);
    }

    [HttpPost]
    public async Task<ActionResult<PapCodeView>> Create(
        CreatePapCodeCommand command, CancellationToken cancellationToken)
    {
        var view = await svc.AddAsync(command, cancellationToken);
        return CreatedAtAction(nameof(Get), new { code = view.Code }, view);
    }
}

// ── Location Codes ────────────────────────────────────────────────────────────

[ApiController]
[Route("api/location-codes")]
[Authorize]
public sealed class LocationCodesController(LocationCodeService svc) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LocationCodeView>>> List(CancellationToken cancellationToken) =>
        Ok(await svc.ListAsync(cancellationToken));

    [HttpGet("{psgcCode}")]
    public async Task<ActionResult<LocationCodeView>> Get(string psgcCode, CancellationToken cancellationToken)
    {
        var view = await svc.GetAsync(psgcCode, cancellationToken);
        return view is null ? NotFound() : Ok(view);
    }

    [HttpPost]
    public async Task<ActionResult<LocationCodeView>> Create(
        CreateLocationCodeCommand command, CancellationToken cancellationToken)
    {
        var view = await svc.AddAsync(command, cancellationToken);
        return CreatedAtAction(nameof(Get), new { psgcCode = view.PsgcCode }, view);
    }
}

// ── Operational Funds ─────────────────────────────────────────────────────────

[ApiController]
[Route("api/operational-funds")]
[Authorize]
public sealed class OperationalFundsController(OperationalFundService svc) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OperationalFundView>>> List(CancellationToken cancellationToken) =>
        Ok(await svc.ListAsync(cancellationToken));

    [HttpGet("{code}")]
    public async Task<ActionResult<OperationalFundView>> Get(string code, CancellationToken cancellationToken)
    {
        var view = await svc.GetAsync(code, cancellationToken);
        return view is null ? NotFound() : Ok(view);
    }

    [HttpPost]
    public async Task<ActionResult<OperationalFundView>> Create(
        CreateOperationalFundCommand command, CancellationToken cancellationToken)
    {
        var view = await svc.AddAsync(command, cancellationToken);
        return CreatedAtAction(nameof(Get), new { code = view.Code }, view);
    }
}
