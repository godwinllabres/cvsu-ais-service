using CvSU.Ais.Api.Auth;
using CvSU.Ais.Application.Collections;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CvSU.Ais.Api.Controllers;

[ApiController]
[Route("api/reports-of-collections")]
[Authorize]
public sealed class ReportOfCollectionsController(RcdService service) : ControllerBase
{
    public sealed record RcdLineRequest(
        string OfficialReceiptName,
        string? OrNumber,
        DateOnly? PostingDate,
        string? Payor,
        string? ModeOfPayment,
        decimal AmountCollected);

    public sealed record CreateRcdRequest(
        DateOnly ReportDate,
        int FiscalYear,
        string CollectingOfficer,
        string DepositSlipNo,
        DateOnly DepositDate,
        string DepositoryBank,
        decimal TotalDeposited,
        IReadOnlyList<RcdLineRequest> Lines,
        string? FundCluster = null,
        string? DepositAccountNumber = null,
        string? Remarks = null);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RcdView>>> List(CancellationToken cancellationToken) =>
        Ok(await service.ListAsync(cancellationToken));

    [HttpPost]
    [Authorize(Policy = CollectionsPolicies.Record)]
    public async Task<ActionResult<RcdDetailView>> Create(
        CreateRcdRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateRcdCommand(
            request.ReportDate,
            request.FiscalYear,
            request.FundCluster,
            request.CollectingOfficer,
            request.DepositSlipNo,
            request.DepositDate,
            request.DepositoryBank,
            request.DepositAccountNumber,
            request.TotalDeposited,
            request.Lines.Select(l => new RcdLineDto(
                l.OfficialReceiptName,
                l.OrNumber,
                l.PostingDate,
                l.Payor,
                l.ModeOfPayment,
                l.AmountCollected)).ToList(),
            request.Remarks);

        var result = await service.CreateAsync(command, cancellationToken);
        return CreatedAtAction(nameof(Get), new { name = result.Name }, result);
    }

    [HttpGet("{name}")]
    public async Task<ActionResult<RcdDetailView>> Get(string name, CancellationToken cancellationToken) =>
        Ok(await service.GetAsync(name, cancellationToken));

    [HttpPost("{name}/deposit")]
    [Authorize(Policy = CollectionsPolicies.Record)]
    public async Task<ActionResult<RcdDetailView>> Deposit(string name, CancellationToken cancellationToken) =>
        Ok(await service.DepositAsync(name, cancellationToken));

    [HttpPost("{name}/cancel")]
    [Authorize(Policy = CollectionsPolicies.Record)]
    public async Task<ActionResult<RcdDetailView>> Cancel(string name, CancellationToken cancellationToken) =>
        Ok(await service.CancelAsync(name, cancellationToken));
}
