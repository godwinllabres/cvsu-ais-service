using CvSU.Ais.Api.Auth;
using CvSU.Ais.Application.Collections;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CvSU.Ais.Api.Controllers;

[ApiController]
[Route("api/orders-of-payment")]
[Authorize]
public sealed class OrdersOfPaymentController(OrderOfPaymentService service) : ControllerBase
{
    public sealed record CreateOrderOfPaymentRequest(
        DateOnly OrderDate,
        string Customer,
        string Description,
        decimal Amount,
        string? FundCluster = null,
        string? IssuedBy = null,
        string? Remarks = null);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderOfPaymentView>>> List(CancellationToken cancellationToken) =>
        Ok(await service.ListAsync(cancellationToken));

    [HttpPost]
    [Authorize(Policy = CollectionsPolicies.Record)]
    public async Task<ActionResult<OrderOfPaymentDetailView>> Create(
        CreateOrderOfPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateOrderOfPaymentCommand(
            request.OrderDate,
            request.Customer,
            request.Description,
            request.Amount,
            request.FundCluster,
            request.IssuedBy,
            request.Remarks);

        var result = await service.CreateAsync(command, cancellationToken);
        return CreatedAtAction(nameof(Get), new { name = result.Name }, result);
    }

    [HttpGet("{name}")]
    public async Task<ActionResult<OrderOfPaymentDetailView>> Get(string name, CancellationToken cancellationToken) =>
        Ok(await service.GetAsync(name, cancellationToken));

    [HttpPost("{name}/issue")]
    [Authorize(Policy = CollectionsPolicies.Record)]
    public async Task<ActionResult<OrderOfPaymentDetailView>> Issue(string name, CancellationToken cancellationToken) =>
        Ok(await service.IssueAsync(name, cancellationToken));

    [HttpPost("{name}/cancel")]
    [Authorize(Policy = CollectionsPolicies.Record)]
    public async Task<ActionResult<OrderOfPaymentDetailView>> Cancel(string name, CancellationToken cancellationToken) =>
        Ok(await service.CancelAsync(name, cancellationToken));
}
