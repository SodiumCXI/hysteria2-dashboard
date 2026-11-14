using Hysteria2Dashboard.Application.DTOs;
using Hysteria2Dashboard.Application.Interfaces;
using Hysteria2Dashboard.Application.Services.Interfaces;
using Hysteria2Dashboard.Domain.Entities;
using Hysteria2Dashboard.Domain.Interfaces;
using Hysteria2Dashboard.Domain.ValueObjects;

namespace Hysteria2Dashboard.Application.Services;

public class UserService(
    IUserRepository userRepository,
    IHysteriaSettingsStore hysteriaSettingsStore,
    IKeySettingsStore keySettingsStore,
    IHysteriaService hysteriaService,
    ITrafficRepository trafficRepository) : IUserService
{
    public async Task<List<UserDto>> GetUsersAsync()
    {
        var users = await userRepository.GetAllUsersAsync();
        var settings = await hysteriaSettingsStore.GetHysteriaSettingsAsync();
        var keySettings = await keySettingsStore.GetKeySettingsAsync();

        return [.. users.Select(u => new UserDto(u.Username, BuildKey(u, settings, keySettings)))];
    }

    public async Task<UserDto> CreateUserAsync(CreateUserDto dto)
    {
        var username = Username.From(dto.Username);

        var exists = await userRepository.ExistsAsync(username);
        if (exists)
            throw new InvalidOperationException($"User '{username}' already exists");

        var settings = await hysteriaSettingsStore.GetHysteriaSettingsAsync();
        var keySettings = await keySettingsStore.GetKeySettingsAsync();
        var user = new User(username, GeneratePassword());
        await userRepository.AddUserAsync(user);
        await hysteriaService.RestartHysteriaAsync();
        return new UserDto(user.Username, BuildKey(user, settings, keySettings));
    }

    public async Task RemoveUserAsync(string username)
    {
        var users = await userRepository.GetAllUsersAsync();

        if (users.Count <= 1)
            throw new InvalidOperationException("At least one user must remain in the Hysteria2 configuration");

        var user = users.FirstOrDefault(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase))?.Username
            ?? throw new InvalidOperationException($"User '{username}' not found");

        await userRepository.DeleteUserAsync(user);
        await hysteriaService.RestartHysteriaAsync();
        await trafficRepository.DeleteAsync(user);
    }

    private static string BuildKey(User user, HysteriaSettings settings, KeySettings keySettings)
    {
        var host = keySettings.ServerIP.Value.Contains(':') ? $"[{keySettings.ServerIP}]" : keySettings.ServerIP.Value;

        return $"hysteria2://{Uri.EscapeDataString(user.Username)}:{Uri.EscapeDataString(user.Password)}@{host}:{settings.Port}" +
               $"?obfs=salamander&obfs-password={Uri.EscapeDataString(settings.ObfsPassword)}" +
               (keySettings.SNI.Value.Length > 0 ? $"&sni={keySettings.SNI}&insecure=1" : string.Empty) +
               $"#{Uri.EscapeDataString($"{keySettings.KeyName}-{user.Username}")}";
    }

    private static string GeneratePassword()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
        return new string([.. bytes.Select(b => chars[b % chars.Length])]);
    }
}
