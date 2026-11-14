namespace Hysteria2Dashboard.Application.Interfaces;

public interface IAppConfigStore
{
    Task<string> GetJwtSecretAsync();
    Task<string> GetTrafficApiSecretAsync();
    Task<string> GetAdminPasswordHashAsync();
    Task<string> GetRouteSaltAsync();
    Task ChangeJwtSecretAsync();
    Task SaveAdminPasswordHashAsync(string password);
    Task SaveRouteSaltAsync(string salt);
}
