using Hysteria2Dashboard.Application.DTOs;
using Hysteria2Dashboard.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hysteria2Dashboard.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SettingsController(ISettingsService settingsService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        try
        {
            var settings = await settingsService.GetSettingsAsync();
            return Ok(settings);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut]
    public async Task<IActionResult> SaveSettings([FromBody] SettingsDto dto)
    {
        try
        {
            var newUrl = await settingsService.SaveSettingsAsync(dto);
            return string.IsNullOrEmpty(newUrl) ? NoContent() : Ok(new { newUrl });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
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

    [HttpPut("password")]
    public async Task<IActionResult> SavePassword([FromBody] ChangePasswordDto password)
    {
        try
        {
            var newToken = await settingsService.SavePasswordAsync(password);
            return Ok(new { newToken });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
