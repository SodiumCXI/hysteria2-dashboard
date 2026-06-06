using Hysteria2Dashboard.Domain.ValueObjects;

namespace Hysteria2Dashboard.Domain.Entities;

public class KeySettings(string serverIP, string keyName)
{
    public IpAddress ServerIP { get; } = IpAddress.From(serverIP);
    public NonEmptyString64 KeyName { get; } = NonEmptyString64.From(keyName, nameof(KeyName));
}
