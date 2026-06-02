namespace Hysteria2Dashboard.Application.Interfaces;

public interface ITrafficProvider
{
    Task<Dictionary<string, (long TxBytes, long RxBytes)>> GetRawTrafficAsync();
}
