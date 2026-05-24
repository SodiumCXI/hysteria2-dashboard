using Hysteria2Dashboard.Application.DTOs;
using Hysteria2Dashboard.Application.Interfaces;
using Hysteria2Dashboard.Application.Services.Interfaces;
using Hysteria2Dashboard.Domain.Entities;
using Hysteria2Dashboard.Domain.Interfaces;

namespace Hysteria2Dashboard.Application.Services;

public class UserService(
    IUserRepository userRepository,
    IHysteriaSettingsStore hysteriaSettingsStore,
    IKeySettingsStore keySettingsStore,
    IHysteriaService hysteriaService) : IUserService
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
        var settings = await hysteriaSettingsStore.GetHysteriaSettingsAsync();
        var keySettings = await keySettingsStore.GetKeySettingsAsync();

        var user = new User(dto.Username, GeneratePassword());

        await userRepository.AddUserAsync(user);
        await hysteriaService.RestartAsync();

        return new UserDto(user.Username, BuildKey(user, settings, keySettings));
    }

    public async Task RemoveUserAsync(string username)
    {
        var users = await userRepository.GetAllUsersAsync();
        var user = users.FirstOrDefault(u => u.Username == username)?.Username
            ?? throw new InvalidOperationException($"User '{username}' not found");

        await userRepository.DeleteUserAsync(user);
        await hysteriaService.RestartAsync();
    }

    private static string BuildKey(User user, HysteriaSettings settings, KeySettings keySettings)
    {
        return $"hysteria2://{user.Username}:{user.Password}@{keySettings.ServerIP}:{settings.Port}" +
               $"?obfs=salamander&obfs-password={settings.ObfsPassword}" +
               $"&sni={settings.SNI}&insecure=1" +
               $"#{keySettings.KeyName}-{user.Username}";
    }

    private static string GeneratePassword()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
        return new string([.. bytes.Select(b => chars[b % chars.Length])]);
    }
}