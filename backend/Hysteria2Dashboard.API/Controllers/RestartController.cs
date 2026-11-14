using Hysteria2Dashboard.Application.DTOs;
using Hysteria2Dashboard.Application.Interfaces;
using Hysteria2Dashboard.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hysteria2Dashboard.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RestartController(IHysteriaService hysteriaService, ISettingsService settingsService) : ControllerBase
{
    [HttpPost("hysteria")]
    public async Task<IActionResult> RestartHysteria()
    {
        try
        {
            await hysteriaService.RestartHysteriaAsync();
            return Ok();
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("dashboard")]
    public async Task<IActionResult> RestartDashboard([FromBody] DashboardSettingsDto dto)
    {
        try
        {
            await settingsService.SaveDashboardSettingsAsync(dto);
            return Ok();
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
}
