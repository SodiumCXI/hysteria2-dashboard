namespace Hysteria2Dashboard.Domain.ValueObjects;

public sealed class Port
{
    public string Value { get; }

    private Port(string value) => Value = value;

    public static Port From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Port cannot be empty");

        if (!ushort.TryParse(value, out ushort port) || port == 0)
            throw new ArgumentException("Port must be a number between 1 and 65535");

        return new Port(value);
    }

    public override string ToString() => Value;

    public static implicit operator string(Port port) => port.Value;
}