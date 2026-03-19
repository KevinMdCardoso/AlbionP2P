using AlbionP2P.Application.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace AlbionP2P.Application;

public static class ApplicationExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateOrderHandler>();
        services.AddScoped<CancelOrderHandler>();
        services.AddScoped<GetRecentOrdersHandler>();
        services.AddScoped<GetMyOrdersHandler>();
        services.AddScoped<CreateDealHandler>();
        services.AddScoped<AcceptDealBySeller>();
        services.AddScoped<AcceptDealByBuyer>();
        services.AddScoped<RejectDealHandler>();
        services.AddScoped<CompleteDealHandler>();
        services.AddScoped<AddRatingHandler>();
        services.AddScoped<GetMyDealsHandler>();
        services.AddScoped<GetDealByIdHandler>();
        services.AddScoped<GetDealMessagesHandler>();
        services.AddScoped<GetUserProfileHandler>();
        return services;
    }
}
