namespace Hysteria2Dashboard.Application.Interfaces;

public interface ITrafficRepository
{
    Task<Dictionary<string, (long TxBytes, long RxBytes)>> GetAllAsync();
    Task AddAsync(Dictionary<string, (long TxBytes, long RxBytes)> traffic);
    Task DeleteAsync(string username);
}
