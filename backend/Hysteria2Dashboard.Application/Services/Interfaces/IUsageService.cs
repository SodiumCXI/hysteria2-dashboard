using Hysteria2Dashboard.Application.DTOs;

namespace Hysteria2Dashboard.Application.Services.Interfaces;

public interface IUsageService
{
    Task<UsageDto> GetUsageAsync();
}
