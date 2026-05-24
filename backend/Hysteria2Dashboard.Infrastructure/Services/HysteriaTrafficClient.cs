using System.Text.Json;
using System.Text.Json.Serialization;
using Hysteria2Dashboard.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Hysteria2Dashboard.Infrastructure.Services;

public class HysteriaTrafficClient(
    HttpClient httpClient,
    IAppConfigStore appConfigStore,
    IConfiguration configuration) : ITrafficSource
{
    private readonly string _baseUrl = configuration["Hysteria2:TrafficApiUrl"] ?? "http://localhost:9999";

    public async Task<Dictionary<string, (long TxBytes, long RxBytes)>> GetRawTrafficAsync()
    {
        var secret = await appConfigStore.GetTrafficApiSecretAsync();

        var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/traffic");
        request.Headers.Add("Authorization", secret);

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(request);
        }
        catch
        {
            return [];
        }

        if (!response.IsSuccessStatusCode)
            return [];

        var json = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<Dictionary<string, TrafficData>>(json);

        if (data is null)
            return [];

        return data.ToDictionary(
            kv => kv.Key,
            kv => (kv.Value.Tx, kv.Value.Rx)
        );
    }

    private class TrafficData
    {
        [JsonPropertyName("tx")]
        public long Tx { get; set; }

        [JsonPropertyName("rx")]
        public long Rx { get; set; }
    }
}