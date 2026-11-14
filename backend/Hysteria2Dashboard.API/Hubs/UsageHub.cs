using Hysteria2Dashboard.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Hysteria2Dashboard.API.Hubs;

[Authorize]
public class UsageHub : Hub
{
    public const string ReceiveUsage = "ReceiveUsage";
}

public class UsageBroadcastService(
    IServiceProvider serviceProvider,
    IHubContext<UsageHub> hubContext) : BackgroundService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly IHubContext<UsageHub> _hubContext = hubContext;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var usageService = scope.ServiceProvider.GetRequiredService<IUsageService>();

                var usage = await usageService.GetUsageAsync();

                await _hubContext.Clients.All.SendAsync(
                    UsageHub.ReceiveUsage,
                    usage,
                    stoppingToken
                );
            }
            catch { /* ignore */ }
        }
    }
}
