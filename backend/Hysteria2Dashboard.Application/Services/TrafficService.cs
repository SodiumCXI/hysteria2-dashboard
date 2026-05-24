using Hysteria2Dashboard.Application.DTOs;
using Hysteria2Dashboard.Application.Interfaces;
using Hysteria2Dashboard.Application.Services.Interfaces;

namespace Hysteria2Dashboard.Application.Services;

public class TrafficService(ITrafficSource trafficSource) : ITrafficService
{
    public async Task<List<TrafficDto>> GetTrafficAsync()
    {
        var raw = await trafficSource.GetRawTrafficAsync();

        return [.. raw.Select(kv => new TrafficDto(
            kv.Key,
            kv.Value.TxBytes,
            kv.Value.RxBytes
        ))];
    }
}