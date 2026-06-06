using Hysteria2Dashboard.Domain.ValueObjects;

namespace Hysteria2Dashboard.Domain.Entities;

public class User(string username, string password)
{
    public NonEmptyString64 Username { get; } = NonEmptyString64.From(username, nameof(Username));
    public NonEmptyString64 Password { get; } = NonEmptyString64.From(password, nameof(Password));
}
