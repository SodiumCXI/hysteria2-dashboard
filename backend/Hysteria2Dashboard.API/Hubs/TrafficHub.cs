using Hysteria2Dashboard.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Hysteria2Dashboard.API.Hubs;

[Authorize]
public class TrafficHub : Hub
{
    public const string ReceiveTraffic = "ReceiveTraffic";
}

public class TrafficBroadcastService(
    IServiceProvider serviceProvider,
    IHubContext<TrafficHub> hubContext) : BackgroundService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly IHubContext<TrafficHub> _hubContext = hubContext;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var trafficService = scope.ServiceProvider.GetRequiredService<ITrafficService>();

                var traffic = await trafficService.GetTrafficAsync();

                if (traffic.Count == 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                    continue;
                }

                await _hubContext.Clients.All.SendAsync(
                    TrafficHub.ReceiveTraffic,
                    traffic,
                    stoppingToken
                );
            }
            catch { /* ignore */ }

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }
}