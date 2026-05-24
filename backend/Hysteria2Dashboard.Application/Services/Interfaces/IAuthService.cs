namespace Hysteria2Dashboard.Application.Services.Interfaces;

public interface IAuthService
{
    Task<string> LoginAsync(string password);
}
