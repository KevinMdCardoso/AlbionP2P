using AlbionP2P.Application.Commands;
using AlbionP2P.Application.DTOs;
using AlbionP2P.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AlbionP2P.API.Controllers;

[ApiController, Route("api/[controller]"), Produces("application/json")]
public class UsersController(GetUserProfileHandler profileH) : ControllerBase
{
    [HttpGet("{userId}"), AllowAnonymous]
    public async Task<IActionResult> GetProfile(string userId, CancellationToken ct = default)
    {
        try
        {
            var profile = await profileH.HandleAsync(userId, ct);
            return Ok(ApiResponse<UserProfileDto>.Ok(profile));
        }
        catch (DomainException ex)
        {
            return NotFound(ApiResponse<UserProfileDto>.Fail(ex.Message));
        }
    }
}
