namespace Hysteria2Dashboard.Domain.ValueObjects;

public sealed class IpAddress
{
    public string Value { get; }

    private IpAddress(string value) => Value = value;

    public static IpAddress From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("IP address cannot be empty");

        if (!System.Net.IPAddress.TryParse(value, out _))
            throw new ArgumentException("Invalid IP address");

        return new IpAddress(value);
    }

    public override string ToString() => Value;

    public static implicit operator string(IpAddress ip) => ip.Value;
}