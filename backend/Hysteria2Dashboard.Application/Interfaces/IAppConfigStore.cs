using Hysteria2Dashboard.Domain.Entities;

namespace Hysteria2Dashboard.Application.Interfaces;

public interface IAppConfigStore
{
    Task<string> GetJwtSecretAsync();
    Task<string> GetTrafficApiSecretAsync();
    Task<string> GetAdminPasswordHashAsync();
    Task<string> GetRouteSaltAsync();
    Task SaveAdminPasswordHashAsync(string hash);
}