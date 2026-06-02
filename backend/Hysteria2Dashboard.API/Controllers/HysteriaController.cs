using Hysteria2Dashboard.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hysteria2Dashboard.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HysteriaController(IHysteriaService hysteriaService) : ControllerBase
{
    [HttpPost("restart")]
    public async Task<IActionResult> Restart()
    {
        try
        {
            await hysteriaService.RestartAsync();
            return Ok();
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

}
