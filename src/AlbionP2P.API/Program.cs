using AlbionP2P.Application;
using AlbionP2P.Domain.Aggregates;
using AlbionP2P.Infrastructure;
using AlbionP2P.Infrastructure.Persistence;
using AlbionP2P.API.Hubs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

builder.Services
    .AddIdentity<AppUser, IdentityRole>(o =>
    {
        o.Password.RequiredLength         = 8;
        o.Password.RequireDigit           = true;
        o.Password.RequireUppercase       = true;
        o.Password.RequireNonAlphanumeric = false;
        o.User.RequireUniqueEmail         = true;
        o.SignIn.RequireConfirmedAccount  = false;
    })
    .AddEntityFrameworkStores<AlbionDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(o =>
{
    o.Cookie.HttpOnly     = true;
    o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    o.Cookie.SameSite     = SameSiteMode.Lax;          // Lax = funciona com Blazor WASM
    o.ExpireTimeSpan      = TimeSpan.FromDays(7);
    o.SlidingExpiration   = true;
    o.Events.OnRedirectToLogin = ctx =>
    {
        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    o.Events.OnRedirectToAccessDenied = ctx =>
    {
        ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

builder.Services.AddSignalR();
builder.Services.AddControllers();

builder.Services.AddCors(o =>
    o.AddPolicy("DevPolicy", p => p
        .WithOrigins(
            "https://localhost:7002",   // Blazor WASM (HTTPS)
            "http://localhost:5002",    // Blazor WASM (HTTP)
            "http://localhost:5000",
            "https://localhost:5001")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "Albion P2P API",
        Version     = "v1",
        Description = "Marketplace P2P para negociações diretas no Albion Online."
    }));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Albion P2P v1"));
}

app.UseHttpsRedirection();
app.UseCors("DevPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AlbionDbContext>();
    db.Database.Migrate();
}

app.Run();
