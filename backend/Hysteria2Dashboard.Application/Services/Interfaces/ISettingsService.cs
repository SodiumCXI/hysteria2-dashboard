using Hysteria2Dashboard.Application.DTOs;

namespace Hysteria2Dashboard.Application.Services.Interfaces;

public interface ISettingsService
{
    Task<SettingsDto> GetSettingsAsync();
    Task<string> SaveSettingsAsync(SettingsDto dto);
    Task SaveDashboardSettingsAsync(DashboardSettingsDto dto);
    Task<string> SavePasswordAsync(ChangePasswordDto dto);
}
