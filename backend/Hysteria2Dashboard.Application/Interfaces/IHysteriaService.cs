using Hysteria2Dashboard.Domain.ValueObjects;

namespace Hysteria2Dashboard.Application.Interfaces;

public interface IHysteriaService
{
    Task<string> GetRawStatusAsync();
    Task<bool> IsUdpPortFreeAsync(Port port);
    Task SwitchUdpPortAsync(Port oldPort, Port newPort);
    Task RestartHysteriaAsync();
}
