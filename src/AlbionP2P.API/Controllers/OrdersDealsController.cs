using AlbionP2P.Application.Commands;
using AlbionP2P.Application.DTOs;
using AlbionP2P.Domain.Exceptions;
using AlbionP2P.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AlbionP2P.API.Controllers;

[ApiController, Route("api/[controller]"), Authorize, Produces("application/json")]
public class OrdersController(
    CreateOrderHandler   createH,
    CancelOrderHandler   cancelH,
    GetRecentOrdersHandler recentH,
    GetMyOrdersHandler   myH) : ControllerBase
{
    string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet, AllowAnonymous]
    public async Task<IActionResult> GetRecent(
        [FromQuery] ItemCategory? category, [FromQuery] OrderType? type,
        [FromQuery] ServerRegion? region,   [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,      CancellationToken ct = default)
    {
        var result = await recentH.HandleAsync(category, type, region, page, pageSize, ct);
        return Ok(ApiResponse<OrderListDto>.Ok(result));
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(CancellationToken ct = default)
        => Ok(ApiResponse<List<OrderDto>>.Ok(await myH.HandleAsync(Uid, ct)));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest req, CancellationToken ct = default)
    {
        try   { return StatusCode(201, ApiResponse<OrderDto>.Ok(await createH.HandleAsync(Uid, req, ct))); }
        catch (DomainException ex) { return BadRequest(ApiResponse<OrderDto>.Fail(ex.Message)); }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct = default)
    {
        try   { await cancelH.HandleAsync(id, Uid, ct); return Ok(ApiResponse<object>.Ok(new { message = "Pedido cancelado." })); }
        catch (DomainException ex) { return BadRequest(ApiResponse<object>.Fail(ex.Message)); }
    }
}

[ApiController, Route("api/[controller]"), Authorize, Produces("application/json")]
public class DealsController(
    CreateDealHandler     createH,
    AcceptDealBySeller    acceptSellerH,
    AcceptDealByBuyer     acceptBuyerH,
    RejectDealHandler     rejectH,
    CompleteDealHandler   completeH,
    AddRatingHandler      ratingH,
    GetMyDealsHandler     myH,
    GetDealByIdHandler    byIdH,
    GetDealMessagesHandler msgsH) : ControllerBase
{
    string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(CancellationToken ct = default)
        => Ok(ApiResponse<List<DealDto>>.Ok(await myH.HandleAsync(Uid, ct)));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        try   { return Ok(ApiResponse<DealDto>.Ok(await byIdH.HandleAsync(id, Uid, ct))); }
        catch (DomainException ex) { return BadRequest(ApiResponse<DealDto>.Fail(ex.Message)); }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDealRequest req, CancellationToken ct = default)
    {
        try   { return StatusCode(201, ApiResponse<DealDto>.Ok(await createH.HandleAsync(Uid, req, ct))); }
        catch (DomainException ex) { return BadRequest(ApiResponse<DealDto>.Fail(ex.Message)); }
    }

    [HttpPost("{id:guid}/accept-seller")]
    public async Task<IActionResult> AcceptBySeller(Guid id, CancellationToken ct = default)
    {
        try   { await acceptSellerH.HandleAsync(id, Uid, ct); return Ok(ApiResponse<object>.Ok(new { message = "Proposta aceita pelo vendedor." })); }
        catch (DomainException ex) { return BadRequest(ApiResponse<object>.Fail(ex.Message)); }
    }

    [HttpPost("{id:guid}/accept-buyer")]
    public async Task<IActionResult> AcceptByBuyer(Guid id, CancellationToken ct = default)
    {
        try   { await acceptBuyerH.HandleAsync(id, Uid, ct); return Ok(ApiResponse<object>.Ok(new { message = "Negociação aceita por ambos!" })); }
        catch (DomainException ex) { return BadRequest(ApiResponse<object>.Fail(ex.Message)); }
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, CancellationToken ct = default)
    {
        try   { await rejectH.HandleAsync(id, Uid, ct); return Ok(ApiResponse<object>.Ok(new { message = "Proposta rejeitada." })); }
        catch (DomainException ex) { return BadRequest(ApiResponse<object>.Fail(ex.Message)); }
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct = default)
    {
        try   { await completeH.HandleAsync(id, ct); return Ok(ApiResponse<object>.Ok(new { message = "Deal concluído! Você pode avaliar agora." })); }
        catch (DomainException ex) { return BadRequest(ApiResponse<object>.Fail(ex.Message)); }
    }

    [HttpPost("{id:guid}/rate")]
    public async Task<IActionResult> Rate(Guid id, [FromBody] CreateRatingRequest req, CancellationToken ct = default)
    {
        try   { await ratingH.HandleAsync(id, Uid, req.Stars, req.Comment, ct); return Ok(ApiResponse<object>.Ok(new { message = "Avaliação registrada!" })); }
        catch (DomainException ex) { return BadRequest(ApiResponse<object>.Fail(ex.Message)); }
    }

    [HttpGet("{id:guid}/messages")]
    public async Task<IActionResult> GetMessages(Guid id, CancellationToken ct = default)
    {
        try   { return Ok(ApiResponse<List<MessageDto>>.Ok(await msgsH.HandleAsync(id, Uid, ct))); }
        catch (DomainException ex) { return BadRequest(ApiResponse<List<MessageDto>>.Fail(ex.Message)); }
    }
}
