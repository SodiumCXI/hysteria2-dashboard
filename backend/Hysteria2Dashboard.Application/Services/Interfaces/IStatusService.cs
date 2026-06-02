namespace Hysteria2Dashboard.Application.Services.Interfaces;

public interface IStatusService
{
    Task<byte> GetStatusAsync();
}
