using System.Text.RegularExpressions;

namespace Hysteria2Dashboard.Domain.ValueObjects;

public sealed partial record Username
{
    public string Value { get; }

    private Username(string value) => Value = value;

    public static Username From(string value)
    {
        var name = (value ?? string.Empty).Trim().ToLowerInvariant();

        if (!UsernameRegex().IsMatch(name))
            throw new ArgumentException("Username must be 1-32 characters: a-z, 0-9, _ or -");

        return new Username(name);
    }

    public override string ToString() => Value;

    public static implicit operator string(Username username) => username.Value;

    [GeneratedRegex("^[a-z0-9_-]{1,32}$")]
    private static partial Regex UsernameRegex();
}
