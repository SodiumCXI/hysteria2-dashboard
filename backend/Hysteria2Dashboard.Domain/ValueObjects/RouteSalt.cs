using System.Text.RegularExpressions;

namespace Hysteria2Dashboard.Domain.ValueObjects;

public sealed partial record RouteSalt
{
    public string Value { get; }

    private RouteSalt(string value) => Value = value;

    public static RouteSalt From(string value)
    {
        var _ = NonEmptyString64.From(value, "RouteSalt");

        if (!AlphaNumericRegex().IsMatch(value))
            throw new ArgumentException("Route salt must contain only English letters and digits");

        return new RouteSalt(value);
    }

    public override string ToString() => Value;

    public static implicit operator string(RouteSalt salt) => salt.Value;

    [GeneratedRegex("^[A-Za-z0-9]+$")]
    private static partial Regex AlphaNumericRegex();
}