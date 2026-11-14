using Hysteria2Dashboard.Domain.ValueObjects;

namespace Hysteria2Dashboard.Domain.Entities;

public class HysteriaSettings(string port, string obfsPassword)
{
    public Port Port { get; } = Port.From(port);
    public NonEmptyString64 ObfsPassword { get; } = NonEmptyString64.From(obfsPassword, nameof(ObfsPassword));
}
