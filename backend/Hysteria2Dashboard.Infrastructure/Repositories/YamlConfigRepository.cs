using Hysteria2Dashboard.Domain.Entities;
using Hysteria2Dashboard.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Hysteria2Dashboard.Infrastructure.Repositories;

public class YamlConfigRepository(IConfiguration configuration) : IUserRepository, IHysteriaSettingsStore
{
    private readonly string _configPath = configuration["Hysteria2:ConfigPath"]
            ?? "/etc/hysteria/config.yaml";

    private readonly SemaphoreSlim _lock = new(1, 1);

    private readonly IDeserializer _deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    private readonly ISerializer _serializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    public async Task<List<User>> GetAllUsersAsync()
    {
        var root = await ReadConfig();
        var userpass = root.Auth?.Userpass ?? [];

        return [.. userpass.Select(kv => new User(kv.Key, kv.Value))];
    }

    public async Task AddUserAsync(User user)
    {
        await _lock.WaitAsync();
        try
        {
            var root = await ReadConfig();
            root.Auth ??= new AuthSection();
            root.Auth.Userpass ??= [];
            root.Auth.Userpass[user.Username] = user.Password;
            await WriteConfig(root);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task DeleteUserAsync(string username)
    {
        await _lock.WaitAsync();
        try
        {
            var root = await ReadConfig();
            root.Auth?.Userpass?.Remove(username);
            await WriteConfig(root);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<HysteriaSettings> GetHysteriaSettingsAsync()
    {
        var root = await ReadConfig();

        var port = (root.Listen ?? ":443").TrimStart(':');

        var masqUrl = root.Masquerade?.Proxy?.Url ?? "https://google.com/";
        var sni = masqUrl
            .Replace("https://", "")
            .TrimEnd('/');

        return new HysteriaSettings(
            port,
            sni,
            root.Obfs?.Salamander?.Password ?? string.Empty
        );
    }

    public async Task SaveHysteriaSettingsAsync(HysteriaSettings settings)
    {
        await _lock.WaitAsync();

        try
        {
            var root = await ReadConfig();

            root.Listen = $":{settings.Port}";

            root.Masquerade ??= new MasqueradeSection();
            root.Masquerade.Proxy ??= new ProxySection();
            root.Masquerade.Proxy.Url = $"https://{settings.SNI}/";
            root.Masquerade.Proxy.RewriteHost = true;

            root.Obfs ??= new ObfsSection();
            root.Obfs.Salamander ??= new SalamanderSection();
            root.Obfs.Salamander.Password = settings.ObfsPassword;

            await WriteConfig(root);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<ConfigRoot> ReadConfig()
    {
        var yaml = await File.ReadAllTextAsync(_configPath);
        return _deserializer.Deserialize<ConfigRoot>(yaml);
    }

    private async Task WriteConfig(ConfigRoot root)
    {
        var yaml = _serializer.Serialize(root);
        await File.WriteAllTextAsync(_configPath, yaml);
    }

    private class ConfigRoot
    {
        public string? Listen { get; set; }
        public TlsSection? Tls { get; set; }
        public BandwidthSection? Bandwidth { get; set; }
        public AuthSection? Auth { get; set; }
        public MasqueradeSection? Masquerade { get; set; }
        public ObfsSection? Obfs { get; set; }
        public TrafficStatsSection? TrafficStats { get; set; }
    }

    private class TlsSection
    {
        public string? Cert { get; set; }
        public string? Key { get; set; }
    }

    private class BandwidthSection
    {
        public string? Up { get; set; }
        public string? Down { get; set; }
    }

    private class AuthSection
    {
        public string? Type { get; set; }
        public Dictionary<string, string>? Userpass { get; set; }
    }

    private class MasqueradeSection
    {
        public string? Type { get; set; }
        public ProxySection? Proxy { get; set; }
    }

    private class ProxySection
    {
        public string? Url { get; set; }
        public bool RewriteHost { get; set; }
    }

    private class ObfsSection
    {
        public string? Type { get; set; }
        public SalamanderSection? Salamander { get; set; }
    }

    private class SalamanderSection
    {
        public string? Password { get; set; }
    }

    private class TrafficStatsSection
    {
        public string? Listen { get; set; }
        public string? Secret { get; set; }
    }
}