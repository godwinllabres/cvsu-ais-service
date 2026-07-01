using CvSU.Ais.Api.Auth;
using CvSU.Ais.Application.Collections;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CvSU.Ais.Api.Controllers;

[ApiController]
[Route("api/official-receipts")]
[Authorize]
public sealed class OfficialReceiptsController(OfficialReceiptService service) : ControllerBase
{
    public sealed record CreateOfficialReceiptRequest(
        string OrNumber,
        DateOnly PostingDate,
        string Customer,
        decimal AmountPaid,
        string ModeOfPayment,
        string? OrderOfPaymentName = null,
        string? FundCluster = null,
        string? IncomeAccount = null,
        string? Remarks = null);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OfficialReceiptView>>> List(CancellationToken cancellationToken) =>
        Ok(await service.ListAsync(cancellationToken));

    [HttpPost]
    [Authorize(Policy = CollectionsPolicies.Record)]
    public async Task<ActionResult<OfficialReceiptDetailView>> Create(
        CreateOfficialReceiptRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateOfficialReceiptCommand(
            request.OrNumber,
            request.PostingDate,
            request.OrderOfPaymentName,
            request.Customer,
            request.AmountPaid,
            request.ModeOfPayment,
            request.FundCluster,
            request.IncomeAccount,
            request.Remarks);

        var result = await service.CreateAsync(command, cancellationToken);
        return CreatedAtAction(nameof(Get), new { name = result.Name }, result);
    }

    [HttpGet("{name}")]
    public async Task<ActionResult<OfficialReceiptDetailView>> Get(string name, CancellationToken cancellationToken) =>
        Ok(await service.GetAsync(name, cancellationToken));

    [HttpPost("{name}/close")]
    [Authorize(Policy = CollectionsPolicies.Record)]
    public async Task<ActionResult<OfficialReceiptDetailView>> Close(string name, CancellationToken cancellationToken) =>
        Ok(await service.CloseAsync(name, cancellationToken));

    [HttpPost("{name}/cancel")]
    [Authorize(Policy = CollectionsPolicies.Record)]
    public async Task<ActionResult<OfficialReceiptDetailView>> Cancel(string name, CancellationToken cancellationToken) =>
        Ok(await service.CancelAsync(name, cancellationToken));
}
