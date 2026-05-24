namespace Hysteria2Dashboard.Domain.Entities;

public class HysteriaSettings(string port, string sni, string obfsPassword)
{
    public string Port { get; } = port;
    public string SNI { get; } = sni;
    public string ObfsPassword { get; } = obfsPassword;
}
