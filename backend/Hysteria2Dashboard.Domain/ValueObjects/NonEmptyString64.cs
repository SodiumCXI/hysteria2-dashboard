namespace Hysteria2Dashboard.Domain.ValueObjects;

public sealed class NonEmptyString64
{
    public string Value { get; }

    private NonEmptyString64(string value) => Value = value;

    public static NonEmptyString64 From(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be empty.", paramName);

        if (value.Length > 64)
            throw new ArgumentException($"Value must be 64 characters or less.", paramName);

        return new NonEmptyString64(value);
    }

    public override string ToString() => Value;

    public static implicit operator string(NonEmptyString64 s) => s.Value;
}