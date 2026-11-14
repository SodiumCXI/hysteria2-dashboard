using Hysteria2Dashboard.Application.DTOs;
using Hysteria2Dashboard.Application.Interfaces;
using Hysteria2Dashboard.Application.Services.Interfaces;

namespace Hysteria2Dashboard.Application.Services;

public class TrafficService(ITrafficProvider trafficProvider, ITrafficRepository trafficRepository) : ITrafficService
{
    public async Task<List<TrafficDto>> GetTrafficAsync()
    {
        var collected = await trafficProvider.CollectTrafficAsync();

        if (collected.Count > 0)
            await trafficRepository.AddAsync(collected);

        var total = await trafficRepository.GetAllAsync();

        return [.. total.Select(kv => new TrafficDto(
            kv.Key,
            kv.Value.TxBytes,
            kv.Value.RxBytes
        ))];
    }
}
