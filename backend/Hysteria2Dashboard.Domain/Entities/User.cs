namespace Hysteria2Dashboard.Domain.Entities;

public class User(string username, string password)
{
    public string Username { get; } = username;
    public string Password { get; } = password;
}
