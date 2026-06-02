using Hysteria2Dashboard.Application.Interfaces;
using Hysteria2Dashboard.Application.Services.Interfaces;

namespace Hysteria2Dashboard.Application.Services;

public class StatusService(IHysteriaService hysteriaService) : IStatusService
{
    public async Task<byte> GetStatusAsync()
    {
        var raw = await hysteriaService.GetRawStatusAsync();

        return raw.Trim() switch
        {
            "active" => 0,
            "activating" or "reloading" => 2,
            _ => 1,
        };
    }
}
