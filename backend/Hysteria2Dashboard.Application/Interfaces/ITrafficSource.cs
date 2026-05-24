namespace Hysteria2Dashboard.Application.Interfaces;

public interface ITrafficSource
{
    Task<Dictionary<string, (long TxBytes, long RxBytes)>> GetRawTrafficAsync();
}
