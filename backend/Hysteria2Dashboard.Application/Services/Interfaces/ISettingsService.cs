using Hysteria2Dashboard.Application.DTOs;

namespace Hysteria2Dashboard.Application.Services.Interfaces;

public interface ISettingsService
{
    Task<SettingsDto> GetSettingsAsync();
    Task SaveSettingsAsync(SettingsDto dto);
}
