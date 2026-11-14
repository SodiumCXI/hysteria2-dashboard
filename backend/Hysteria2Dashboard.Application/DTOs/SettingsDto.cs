namespace Hysteria2Dashboard.Application.DTOs;

public record SettingsDto(string HysteriaPort, string SNI, string ObfsPassword, string KeyName, string DashboardPort, string RouteSalt);
