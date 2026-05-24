using Hysteria2Dashboard.Domain.Entities;

namespace Hysteria2Dashboard.Domain.Interfaces;

public interface IHysteriaSettingsStore
{
    Task<HysteriaSettings> GetHysteriaSettingsAsync();
    Task SaveHysteriaSettingsAsync(HysteriaSettings settings);
}
