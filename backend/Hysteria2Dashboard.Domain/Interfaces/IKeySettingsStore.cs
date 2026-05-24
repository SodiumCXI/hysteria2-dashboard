using Hysteria2Dashboard.Domain.Entities;

namespace Hysteria2Dashboard.Domain.Interfaces;

public interface IKeySettingsStore
{
    Task<KeySettings> GetKeySettingsAsync();
    Task SaveKeySettingsAsync(KeySettings settings);
}
