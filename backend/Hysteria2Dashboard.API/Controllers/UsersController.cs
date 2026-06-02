using Hysteria2Dashboard.Application.DTOs;
using Hysteria2Dashboard.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hysteria2Dashboard.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var users = await userService.GetUsersAsync();
            return Ok(users);
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
    {
        try
        {
            var user = await userService.CreateUserAsync(dto);
            return CreatedAtAction(nameof(GetAll), user);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpDelete("{username}")]
    public async Task<IActionResult> Delete(string username)
    {
        try
        {
            await userService.RemoveUserAsync(username);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
