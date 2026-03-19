using AlbionP2P.Application.DTOs;
using AlbionP2P.Domain.Aggregates;
using AlbionP2P.Domain.Exceptions;
using AlbionP2P.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;

namespace AlbionP2P.API.Hubs;

[Authorize]
public class ChatHub(IDealRepository dealRepo, IUnitOfWork uow, UserManager<AppUser> um) : Hub
{
    public async Task JoinDeal(string dealId)
    {
        try
        {
            var userId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(userId)) { await Clients.Caller.SendAsync("Error", "Não autenticado."); return; }

            var deal = await dealRepo.GetByIdAsync(Guid.Parse(dealId));
            if (deal is null || (deal.BuyerId != userId && deal.SellerId != userId))
            {
                await Clients.Caller.SendAsync("Error", "Acesso negado a este deal.");
                return;
            }
            await Groups.AddToGroupAsync(Context.ConnectionId, dealId);
            await Clients.Caller.SendAsync("JoinedDeal", dealId);
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", $"Erro ao entrar no chat: {ex.Message}");
        }
    }

    public async Task LeaveDeal(string dealId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, dealId);

    public async Task SendMessage(string dealId, string content)
    {
        var userId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(userId)) { await Clients.Caller.SendAsync("Error", "Não autenticado."); return; }
        try
        {
            var deal = await dealRepo.GetByIdWithMessagesAsync(Guid.Parse(dealId));
            if (deal is null) { await Clients.Caller.SendAsync("Error", "Negociação não encontrada."); return; }

            var message = deal.AddMessage(userId, content);
            await dealRepo.AddMessageAsync(message);
            await uow.SaveChangesAsync();

            var sender = await um.FindByIdAsync(userId);
            var dto    = new MessageDto(message.MessageId, message.SenderId, sender?.AlbionNick ?? "", message.Content, message.SentAt);
            await Clients.Group(dealId).SendAsync("ReceiveMessage", dto);
        }
        catch (DomainException ex) { await Clients.Caller.SendAsync("Error", ex.Message); }
        catch (Exception ex)       { await Clients.Caller.SendAsync("Error", $"Erro ao enviar mensagem: {ex.Message}"); }
    }
}
