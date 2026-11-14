using Hysteria2Dashboard.Application.DTOs;
using Hysteria2Dashboard.Application.Interfaces;
using Hysteria2Dashboard.Application.Services.Interfaces;

namespace Hysteria2Dashboard.Application.Services;

public class UsageService(IUsageProvider usageProvider) : IUsageService
{
    public async Task<UsageDto> GetUsageAsync()
    {
        var cpuUsage = await usageProvider.GetCpuUsageAsync();
        var ramUsage = await usageProvider.GetRamUsageAsync();

        return new UsageDto(cpuUsage, ramUsage);
    }
}
