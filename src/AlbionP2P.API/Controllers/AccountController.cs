// ═══════════════════════════════════════════════════════════
// AccountController.cs
// ═══════════════════════════════════════════════════════════
using AlbionP2P.Application.DTOs;
using AlbionP2P.Domain.Aggregates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AlbionP2P.API.Controllers;

[ApiController, Route("api/[controller]"), Produces("application/json")]
public class AccountController(UserManager<AppUser> um, SignInManager<AppUser> sm) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        var user   = new AppUser(req.Email, req.AlbionNick, req.ServerRegion);
        var result = await um.CreateAsync(user, req.Password);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<UserDto>.Fail(string.Join("; ", result.Errors.Select(e => e.Description))));
        await sm.SignInAsync(user, isPersistent: true);
        return Ok(ApiResponse<UserDto>.Ok(Map(user)));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var result = await sm.PasswordSignInAsync(req.Email, req.Password, isPersistent: true, lockoutOnFailure: false);
        if (!result.Succeeded) return BadRequest(ApiResponse<UserDto>.Fail("Email ou senha inválidos."));
        var user = await um.FindByEmailAsync(req.Email);
        return Ok(ApiResponse<UserDto>.Ok(Map(user!)));
    }

    [HttpPost("logout"), Authorize]
    public async Task<IActionResult> Logout()
    {
        await sm.SignOutAsync();
        return Ok(ApiResponse<object>.Ok(new { message = "Logout realizado." }));
    }

    [HttpGet("me"), Authorize]
    public async Task<IActionResult> Me()
    {
        var user = await um.GetUserAsync(User);
        return user is null
            ? Unauthorized(ApiResponse<UserDto>.Fail("Não autenticado."))
            : Ok(ApiResponse<UserDto>.Ok(Map(user)));
    }

    static UserDto Map(AppUser u) => new(u.Id, u.Email!, u.AlbionNick, u.ServerRegion, u.Reputation);
}
