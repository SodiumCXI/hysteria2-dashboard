using Hysteria2Dashboard.Application.DTOs;
using Hysteria2Dashboard.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Hysteria2Dashboard.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        try
        {
            var token = await authService.LoginAsync(request.Password);
            return Ok(new { token });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
