using Hysteria2Dashboard.Application.DTOs;
using Hysteria2Dashboard.Application.Interfaces;
using Hysteria2Dashboard.Application.Services.Interfaces;
using Hysteria2Dashboard.Domain.Entities;
using Hysteria2Dashboard.Domain.Interfaces;

namespace Hysteria2Dashboard.Application.Services;

public class SettingsService(
    IHysteriaSettingsStore hysteriaSettingsStore,
    IKeySettingsStore keySettingsStore,
    IHysteriaService hysteriaService) : ISettingsService
{
    public async Task<SettingsDto> GetSettingsAsync()
    {
        var hysteriaSettings = await hysteriaSettingsStore.GetHysteriaSettingsAsync();
        var keySettings = await keySettingsStore.GetKeySettingsAsync();
        return new SettingsDto(hysteriaSettings.Port, hysteriaSettings.SNI, hysteriaSettings.ObfsPassword, keySettings.KeyName);
    }

    public async Task SaveSettingsAsync(SettingsDto dto)
    {
        var hysteriaSettings = new HysteriaSettings(dto.Port, dto.SNI, dto.ObfsPassword);
        var oldKeySettings = await keySettingsStore.GetKeySettingsAsync();
        var newKeySettings = new KeySettings(oldKeySettings.ServerIP, dto.KeyName);

        await hysteriaSettingsStore.SaveHysteriaSettingsAsync(hysteriaSettings);
        await keySettingsStore.SaveKeySettingsAsync(newKeySettings);

        await hysteriaService.RestartAsync();
    }
}