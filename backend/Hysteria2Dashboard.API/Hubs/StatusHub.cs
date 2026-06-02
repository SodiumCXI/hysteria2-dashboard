using Hysteria2Dashboard.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Hysteria2Dashboard.API.Hubs;

[Authorize]
public class StatusHub : Hub
{
    public const string ReceiveStatus = "ReceiveStatus";
}

public class StatusBroadcastService(
    IServiceProvider serviceProvider,
    IHubContext<StatusHub> hubContext) : BackgroundService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly IHubContext<StatusHub> _hubContext = hubContext;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var statusService = scope.ServiceProvider.GetRequiredService<IStatusService>();

                var status = await statusService.GetStatusAsync();

                await _hubContext.Clients.All.SendAsync(
                    StatusHub.ReceiveStatus,
                    status,
                    stoppingToken
                );
            }
            catch { /* ignore */ }
        }
    }
}
