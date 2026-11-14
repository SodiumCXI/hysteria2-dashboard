using System.Text.RegularExpressions;

namespace Hysteria2Dashboard.Domain.ValueObjects;

public sealed partial record Sni
{
    public string Value { get; }

    private Sni(string value) => Value = value;

    public static Sni From(string value)
    {
        var sni = (value ?? string.Empty).Trim();

        if (!HostNameRegex().IsMatch(sni))
            throw new ArgumentException("SNI must be empty or a host name of 64 characters or less");

        return new Sni(sni);
    }

    public override string ToString() => Value;

    public static implicit operator string(Sni sni) => sni.Value;

    [GeneratedRegex("^[A-Za-z0-9.-]{0,64}$")]
    private static partial Regex HostNameRegex();
}
