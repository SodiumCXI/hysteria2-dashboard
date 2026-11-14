using Hysteria2Dashboard.Domain.ValueObjects;

namespace Hysteria2Dashboard.Domain.Entities;

public class KeySettings(string serverIP, string keyName, string sni)
{
    public IpAddress ServerIP { get; } = IpAddress.From(serverIP);
    public NonEmptyString64 KeyName { get; } = NonEmptyString64.From(keyName, nameof(KeyName));
    public Sni SNI { get; } = Sni.From(sni);

    public KeySettings With(string keyName, string sni) => new(ServerIP, keyName, sni);
}
