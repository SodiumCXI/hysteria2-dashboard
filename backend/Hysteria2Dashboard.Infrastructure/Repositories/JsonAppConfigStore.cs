using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hysteria2Dashboard.Application.Interfaces;
using Hysteria2Dashboard.Domain.Entities;
using Hysteria2Dashboard.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Hysteria2Dashboard.Infrastructure.Repositories;

public class JsonAppConfigStore(IConfiguration configuration) : IAppConfigStore, IKeySettingsStore
{
    private readonly string _storePath = configuration["App:StorePath"] ?? "/etc/hysteria/app.json";
    private readonly SemaphoreSlim _lock = new(1, 1);

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    public async Task<string> GetJwtSecretAsync()
        => (await ReadStore()).JwtSecret;

    public async Task<string> GetTrafficApiSecretAsync()
        => (await ReadStore()).TrafficApiSecret;

    public async Task<string> GetAdminPasswordHashAsync()
        => (await ReadStore()).AdminPasswordHash;

    public async Task<string> GetRouteSaltAsync()
        => (await ReadStore()).RouteSalt;

    public async Task SaveAdminPasswordHashAsync(string hash)
    {
        await _lock.WaitAsync();

        try
        {
            var store = await ReadStore();
            store.AdminPasswordHash = hash;
            await WriteStore(store);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<KeySettings> GetKeySettingsAsync()
    {
        var store = await ReadStore();
        return new KeySettings(
            store.ServerIP,
            store.KeyName
        );
    }

    public async Task SaveKeySettingsAsync(KeySettings settings)
    {
        await _lock.WaitAsync();

        try
        {
            var store = await ReadStore();
            store.ServerIP = settings.ServerIP;
            store.KeyName = settings.KeyName;
            await WriteStore(store);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<AppData> ReadStore()
    {
        if (!File.Exists(_storePath))
            throw new InvalidOperationException($"App config not found at {_storePath}. Run the install script first.");

        var json = await File.ReadAllTextAsync(_storePath);
        return JsonSerializer.Deserialize<AppData>(json)
            ?? throw new InvalidOperationException("Failed to parse app config");
    }

    private async Task WriteStore(AppData data)
    {
        var json = JsonSerializer.Serialize(data, _jsonOptions);
        await File.WriteAllTextAsync(_storePath, json);
    }

    private class AppData
    {
        [JsonPropertyName("jwtSecret")]
        public string JwtSecret { get; set; } = string.Empty;

        [JsonPropertyName("trafficApiSecret")]
        public string TrafficApiSecret { get; set; } = string.Empty;

        [JsonPropertyName("routeSalt")]
        public string RouteSalt { get; set; } = string.Empty;

        [JsonPropertyName("adminPasswordHash")]
        public string AdminPasswordHash { get; set; } = string.Empty;

        [JsonPropertyName("serverIP")]
        public string ServerIP { get; set; } = string.Empty;

        [JsonPropertyName("keyName")]
        public string KeyName { get; set; } = string.Empty;
    }
}