namespace Hysteria2Dashboard.Application.Interfaces;

public interface IUsageProvider
{
    Task<int> GetCpuUsageAsync();
    Task<int> GetRamUsageAsync();
}
