namespace Hysteria2Dashboard.Application.DTOs;

public record SettingsDto(string Port, string SNI, string ObfsPassword, string KeyName);
