namespace Hysteria2Dashboard.Application.Interfaces;

public interface IHysteriaService
{
    Task RestartAsync();
    Task<string> GetStatusAsync();
}
