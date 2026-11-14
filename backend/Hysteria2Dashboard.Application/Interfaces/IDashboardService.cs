using Hysteria2Dashboard.Domain.ValueObjects;

namespace Hysteria2Dashboard.Application.Interfaces;

public interface IDashboardService
{
    Task<string> GetDashboardPortAsync();
    Task<bool> IsTcpPortFreeAsync(Port port);
    Task ApplyDashboardSettingsAsync(Port port, RouteSalt salt);
}
