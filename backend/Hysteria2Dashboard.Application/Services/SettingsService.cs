using Hysteria2Dashboard.Application.DTOs;
using Hysteria2Dashboard.Application.Interfaces;
using Hysteria2Dashboard.Application.Services.Interfaces;
using Hysteria2Dashboard.Domain.Entities;
using Hysteria2Dashboard.Domain.Interfaces;
using Hysteria2Dashboard.Domain.ValueObjects;

namespace Hysteria2Dashboard.Application.Services;

public class SettingsService(
    IHysteriaSettingsStore hysteriaSettingsStore,
    IKeySettingsStore keySettingsStore,
    IHysteriaService hysteriaService,
    IDashboardService dashboardService,
    IAppConfigStore appConfigStore,
    IAuthService authService) : ISettingsService
{
    public async Task<SettingsDto> GetSettingsAsync()
    {
        var hysteriaSettings = await hysteriaSettingsStore.GetHysteriaSettingsAsync();
        var keySettings = await keySettingsStore.GetKeySettingsAsync();
        var routeSalt = await appConfigStore.GetRouteSaltAsync();
        var dashboardPort = await dashboardService.GetDashboardPortAsync();
        return new SettingsDto(hysteriaSettings.Port, keySettings.SNI, hysteriaSettings.ObfsPassword, keySettings.KeyName, dashboardPort, routeSalt);
    }

    public async Task<string> SaveSettingsAsync(SettingsDto dto)
    {
        var hysteriaSettings = new HysteriaSettings(dto.HysteriaPort, dto.ObfsPassword);
        var keySettings = (await keySettingsStore.GetKeySettingsAsync()).With(dto.KeyName, dto.SNI);
        var dashboardPort = Port.From(dto.DashboardPort);
        var routeSalt = RouteSalt.From(dto.RouteSalt);

        var oldHysteriaSettings = await hysteriaSettingsStore.GetHysteriaSettingsAsync();
        var hysteriaPortChanged = oldHysteriaSettings.Port.Value != hysteriaSettings.Port.Value;
        if (hysteriaPortChanged && !await hysteriaService.IsUdpPortFreeAsync(hysteriaSettings.Port))
            throw new InvalidOperationException($"UDP port {hysteriaSettings.Port} is already in use");

        var dashboardPortChanged = await dashboardService.GetDashboardPortAsync() != dashboardPort.Value;
        if (dashboardPortChanged && !await dashboardService.IsTcpPortFreeAsync(dashboardPort))
            throw new InvalidOperationException($"TCP port {dashboardPort} is already in use");

        await keySettingsStore.SaveKeySettingsAsync(keySettings);

        if (hysteriaPortChanged || oldHysteriaSettings.ObfsPassword.Value != hysteriaSettings.ObfsPassword.Value)
        {
            await hysteriaSettingsStore.SaveHysteriaSettingsAsync(hysteriaSettings);
            await hysteriaService.RestartHysteriaAsync();

            if (hysteriaPortChanged)
                await hysteriaService.SwitchUdpPortAsync(oldHysteriaSettings.Port, hysteriaSettings.Port);
        }

        if (dashboardPortChanged || await appConfigStore.GetRouteSaltAsync() != routeSalt.Value)
            return $"https://{keySettings.ServerIP}:{dashboardPort}/{routeSalt}/";

        return string.Empty;
    }

    public async Task SaveDashboardSettingsAsync(DashboardSettingsDto dto)
    {
        var dashboardPort = Port.From(dto.DashboardPort);
        var routeSalt = RouteSalt.From(dto.RouteSalt);

        await dashboardService.ApplyDashboardSettingsAsync(dashboardPort, routeSalt);
        await appConfigStore.SaveRouteSaltAsync(routeSalt);
    }

    public async Task<string> SavePasswordAsync(ChangePasswordDto dto)
    {
        var password = NonEmptyString64.From(dto.Password, nameof(dto.Password));
        var hash = BCrypt.Net.BCrypt.HashPassword(password);

        await appConfigStore.SaveAdminPasswordHashAsync(hash);
        await appConfigStore.ChangeJwtSecretAsync();
        return await authService.LoginAsync(password);
    }
}
