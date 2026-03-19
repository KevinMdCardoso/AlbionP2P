using AlbionP2P.Application.DTOs;
using AlbionP2P.Domain.Aggregates;
using AlbionP2P.Domain.Exceptions;
using AlbionP2P.Domain.Interfaces;
using AlbionP2P.Domain.ValueObjects;
using Microsoft.AspNetCore.Identity;

namespace AlbionP2P.Application.Commands;

// ── Orders ────────────────────────────────────────────────────────────────────
public sealed class CreateOrderHandler(IOrderRepository repo, IUnitOfWork uow)
{
    public async Task<OrderDto> HandleAsync(string userId, CreateOrderRequest req, CancellationToken ct = default)
    {
        var order = new Order(userId, req.ItemName, req.Category, req.Quantity, new Money(req.UnitPrice), req.Type, req.ServerRegion);
        await repo.AddAsync(order, ct);
        await uow.SaveChangesAsync(ct);
        return Map(order, "");
    }

    internal static OrderDto Map(Order o, string nick) => new(
        o.OrderId, o.UserId, nick, o.ItemName, o.ItemCategory,
        o.Quantity, o.UnitPrice.Amount, o.Type,
        o.Status.Value.ToString(), o.ServerRegion, o.CreatedAt);
}

public sealed class CancelOrderHandler(IOrderRepository repo, IUnitOfWork uow)
{
    public async Task HandleAsync(Guid orderId, string userId, CancellationToken ct = default)
    {
        var order = await repo.GetByIdAsync(orderId, ct) ?? throw new DomainException("Pedido não encontrado.");
        order.Cancel(userId);
        await repo.UpdateAsync(order, ct);
        await uow.SaveChangesAsync(ct);
    }
}

public sealed class GetRecentOrdersHandler(IOrderRepository repo, UserManager<AppUser> um)
{
    public async Task<OrderListDto> HandleAsync(ItemCategory? cat, OrderType? type, ServerRegion? region, int page, int size, CancellationToken ct = default)
    {
        var orders = await repo.GetRecentAsync(cat, type, region, page, size, ct);
        var dtos   = new List<OrderDto>();
        foreach (var o in orders)
        {
            var user = await um.FindByIdAsync(o.UserId);
            dtos.Add(CreateOrderHandler.Map(o, user?.AlbionNick ?? ""));
        }
        return new OrderListDto(dtos, dtos.Count, page, size);
    }
}

public sealed class GetMyOrdersHandler(IOrderRepository repo, UserManager<AppUser> um)
{
    public async Task<List<OrderDto>> HandleAsync(string userId, CancellationToken ct = default)
    {
        var orders = await repo.GetByUserIdAsync(userId, ct);
        var user   = await um.FindByIdAsync(userId);
        return orders.Select(o => CreateOrderHandler.Map(o, user?.AlbionNick ?? "")).ToList();
    }
}

// ── Deals ─────────────────────────────────────────────────────────────────────
public sealed class CreateDealHandler(IOrderRepository orderRepo, IDealRepository dealRepo, IUnitOfWork uow)
{
    public async Task<DealDto> HandleAsync(string buyerId, CreateDealRequest req, CancellationToken ct = default)
    {
        var order = await orderRepo.GetByIdAsync(req.OrderId, ct) ?? throw new DomainException("Pedido não encontrado.");
        if (!order.Status.IsOpen && !order.Status.IsInNegotiation) throw new DomainException("Pedido não disponível para negociação.");
        if (order.UserId == buyerId) throw new DomainException("Você não pode negociar com o seu próprio pedido.");

        var sellerId      = order.Type == OrderType.Sell ? order.UserId : buyerId;
        var actualBuyerId = order.Type == OrderType.Sell ? buyerId      : order.UserId;

        var deal = new Deal(order.OrderId, actualBuyerId, sellerId, new Money(req.ProposedPrice));
        order.StartNegotiation();

        await dealRepo.AddAsync(deal, ct);
        await orderRepo.UpdateAsync(order, ct);
        await uow.SaveChangesAsync(ct);
        return Map(deal);
    }

    internal static DealDto Map(Deal d) => new(d.DealId, d.OrderId, d.BuyerId, d.SellerId, d.ProposedPrice.Amount, d.Status.ToString(), d.BuyerConfirmed, d.SellerConfirmed, d.CreatedAt);
}

public sealed class AcceptDealBySeller(IDealRepository repo, IOrderRepository orderRepo, IUnitOfWork uow)
{
    public async Task HandleAsync(Guid dealId, string userId, CancellationToken ct = default)
    {
        var deal = await repo.GetByIdAsync(dealId, ct) ?? throw new DomainException("Negociação não encontrada.");

        if (deal.SellerId != userId) 
            throw new DomainException("Apenas o vendedor pode aceitar a proposta.");

        deal.AcceptBySeller();
        await repo.UpdateAsync(deal, ct);
        await uow.SaveChangesAsync(ct);
    }
}

public sealed class AcceptDealByBuyer(IDealRepository repo, IOrderRepository orderRepo, IUnitOfWork uow)
{
    public async Task HandleAsync(Guid dealId, string userId, CancellationToken ct = default)
    {
        var deal = await repo.GetByIdAsync(dealId, ct) ?? throw new DomainException("Negociação não encontrada.");

        if (deal.BuyerId != userId) 
            throw new DomainException("Apenas o comprador pode confirmar a aceitação.");

        deal.AcceptByBuyer();

        // Rejeitar todos os outros deals do mesmo pedido
        var otherDeals = await repo.GetByOrderIdAsync(deal.OrderId, ct);
        foreach (var otherDeal in otherDeals.Where(d => d.DealId != dealId && d.Status != DealStatusValue.Rejected))
        {
            otherDeal.Reject("SYSTEM");
            await repo.UpdateAsync(otherDeal, ct);
        }

        await repo.UpdateAsync(deal, ct);
        await uow.SaveChangesAsync(ct);
    }
}

public sealed class RejectDealHandler(IDealRepository repo, IUnitOfWork uow)
{
    public async Task HandleAsync(Guid dealId, string userId, CancellationToken ct = default)
    {
        var deal = await repo.GetByIdAsync(dealId, ct) ?? throw new DomainException("Negociação não encontrada.");
        deal.Reject(userId);
        await repo.UpdateAsync(deal, ct);
        await uow.SaveChangesAsync(ct);
    }
}

public sealed class CompleteDealHandler(IDealRepository repo, IUnitOfWork uow)
{
    public async Task HandleAsync(Guid dealId, CancellationToken ct = default)
    {
        var deal = await repo.GetByIdAsync(dealId, ct) ?? throw new DomainException("Negociação não encontrada.");
        deal.Complete();
        await repo.UpdateAsync(deal, ct);
        await uow.SaveChangesAsync(ct);
    }
}

public sealed class AddRatingHandler(IDealRepository repo, UserManager<AppUser> um, IRatingRepository ratingRepo, IUnitOfWork uow)
{
    public async Task HandleAsync(Guid dealId, string raterId, int stars, string comment, CancellationToken ct = default)
    {
        var deal = await repo.GetByIdAsync(dealId, ct) ?? throw new DomainException("Negociação não encontrada.");

        var ratedId = raterId == deal.BuyerId ? deal.SellerId : deal.BuyerId;
        deal.AddRating(raterId, ratedId, stars, comment);

        var ratedUser = await um.FindByIdAsync(ratedId);
        if (ratedUser != null)
        {
            ratedUser.AddReputation(stars);
            await um.UpdateAsync(ratedUser);
        }

        // Adicionar o rating ao repositório
        var rating = deal.Ratings.FirstOrDefault(r => r.RaterId == raterId && r.RatedId == ratedId);
        if (rating != null)
        {
            await ratingRepo.AddAsync(rating, ct);
        }

        await repo.UpdateAsync(deal, ct);
        await uow.SaveChangesAsync(ct);
    }
}

public sealed class GetMyDealsHandler(IDealRepository repo)
{
    public async Task<List<DealDto>> HandleAsync(string userId, CancellationToken ct = default)
    {
        var deals = await repo.GetByUserIdAsync(userId, ct);
        return deals.Select(CreateDealHandler.Map).ToList();
    }
}

public sealed class GetDealByIdHandler(IDealRepository repo)
{
    public async Task<DealDto> HandleAsync(Guid dealId, string userId, CancellationToken ct = default)
    {
        var deal = await repo.GetByIdAsync(dealId, ct) ?? throw new DomainException("Negociação não encontrada.");
        if (deal.BuyerId != userId && deal.SellerId != userId) throw new DomainException("Acesso negado.");
        return CreateDealHandler.Map(deal);
    }
}

public sealed class GetDealMessagesHandler(IDealRepository repo, UserManager<AppUser> um)
{
    public async Task<List<MessageDto>> HandleAsync(Guid dealId, string userId, CancellationToken ct = default)
    {
        var deal = await repo.GetByIdWithMessagesAsync(dealId, ct) ?? throw new DomainException("Negociação não encontrada.");
        if (deal.BuyerId != userId && deal.SellerId != userId) throw new DomainException("Acesso negado.");
        var dtos = new List<MessageDto>();
        foreach (var m in deal.Messages.OrderBy(x => x.SentAt))
        {
            var sender = await um.FindByIdAsync(m.SenderId);
            dtos.Add(new MessageDto(m.MessageId, m.SenderId, sender?.AlbionNick ?? "", m.Content, m.SentAt));
        }
        return dtos;
    }
}

public sealed class GetUserProfileHandler(UserManager<AppUser> um, IRatingRepository ratingRepo)
{
    public async Task<UserProfileDto> HandleAsync(string userId, CancellationToken ct = default)
    {
        var user = await um.FindByIdAsync(userId) ?? throw new DomainException("Usuário não encontrado.");
        var ratings = await ratingRepo.GetByUserIdAsync(userId, ct);

        var ratingDtos = ratings.Select(r => new RatingDto(r.Id, r.DealId, r.RaterId, r.RatedId, r.Stars, r.Comment, r.CreatedAt)).ToList();
        var averageRating = ratings.Count > 0 ? ratings.Average(r => r.Stars) : 0;

        var userDto = new UserDto(user.Id, user.Email ?? "", user.AlbionNick, user.ServerRegion, user.Reputation);
        return new UserProfileDto(userDto, ratingDtos, averageRating);
    }
}
