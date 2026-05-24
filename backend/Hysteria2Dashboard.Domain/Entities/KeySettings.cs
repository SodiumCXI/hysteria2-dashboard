namespace Hysteria2Dashboard.Domain.Entities;

public class KeySettings(string serverIP, string keyName)
{
    public string ServerIP { get; } = serverIP;
    public string KeyName { get; } = keyName;
}
