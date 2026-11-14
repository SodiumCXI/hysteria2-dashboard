using Hysteria2Dashboard.Application.Interfaces;
using Hysteria2Dashboard.Domain.ValueObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Renci.SshNet;

namespace Hysteria2Dashboard.Infrastructure.Services;

public class SshServerService : IHysteriaService, IDashboardService, IUsageProvider, IDisposable
{
    private readonly SshClient? _client;
    private readonly string _ctl = string.Empty;
    private readonly IHostEnvironment _environment;
    private readonly object _connectLock = new();

    public SshServerService(IConfiguration config, IHostEnvironment environment)
    {
        _environment = environment;

        if (_environment.IsDevelopment())
            return;

        _ctl = $"sudo {config["App:DashboardPath"] ?? "/opt/hysteria2-dashboard"}/ctl.sh";

        var host = config["SSH:Host"] ?? "host.docker.internal";
        var user = config["SSH:User"] ?? "hysteria";
        var keyPath = config["SSH:KeyPath"] ?? "/app/ssh_key";
        var port = int.Parse(config["SSH:Port"] ?? "22");

        var keyFile = new PrivateKeyFile(keyPath);
        var authMethod = new PrivateKeyAuthenticationMethod(user, keyFile);
        var connInfo = new ConnectionInfo(host, port, user, authMethod);

        _client = new SshClient(connInfo);
    }

    public async Task RestartHysteriaAsync()
    {
        if (_environment.IsDevelopment())
            return;

        await RunCommandAsync($"{_ctl} restart");
    }

    public async Task<bool> IsTcpPortFreeAsync(Port port)
    {
        if (_environment.IsDevelopment())
            return true;

        var result = await RunCommandAsync($"ss -tln | grep -q ':{port} ' && echo busy || echo free");
        return result.Trim() == "free";
    }

    public async Task<bool> IsUdpPortFreeAsync(Port port)
    {
        if (_environment.IsDevelopment())
            return true;

        var result = await RunCommandAsync($"ss -uln | grep -q ':{port} ' && echo busy || echo free");
        return result.Trim() == "free";
    }

    public async Task SwitchUdpPortAsync(Port oldPort, Port newPort)
    {
        if (_environment.IsDevelopment())
            return;

        await RunCommandAsync($"{_ctl} firewall {oldPort} {newPort}");
    }

    public async Task<string> GetRawStatusAsync()
    {
        if (_environment.IsDevelopment())
        {
            string[] statuses = ["active", "inactive", "activating"];
            return statuses[Random.Shared.Next(statuses.Length)];
        }

        var output = await RunCommandAsync("systemctl is-active hysteria-server || true");
        return output.Trim();
    }

    public async Task<int> GetCpuUsageAsync()
    {
        if (_environment.IsDevelopment())
        {
            await Task.Delay(500);
            return Random.Shared.Next(0, 101);
        }

        var s1 = await RunCommandAsync("grep '^cpu ' /proc/stat");
        await Task.Delay(500);
        var s2 = await RunCommandAsync("grep '^cpu ' /proc/stat");

        var t1 = ParseCpuStats(s1);
        var t2 = ParseCpuStats(s2);

        var idle = t2.idle - t1.idle;
        var total = t2.total - t1.total;

        return total <= 0 ? 0 : (int)Math.Round((1.0 - (double)idle / total) * 100);
    }

    public async Task<int> GetRamUsageAsync()
    {
        if (_environment.IsDevelopment())
            return Random.Shared.Next(0, 101);

        var result = await RunCommandAsync("grep -E '^(MemTotal|MemAvailable):' /proc/meminfo");
        var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        var total = ParseRamLine(lines.First(l => l.StartsWith("MemTotal")));
        var available = ParseRamLine(lines.First(l => l.StartsWith("MemAvailable")));

        return (int)Math.Round((double)(total - available) / total * 100);
    }

    private static (long idle, long total) ParseCpuStats(string line)
    {
        var values = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)
                         .Skip(1).Select(long.Parse).ToArray();
        return (values[3], values.Sum());
    }

    private static long ParseRamLine(string line)
    {
        return long.Parse(line.Split(':')[1].Trim().Split(' ')[0]);
    }

    public async Task<string> GetDashboardPortAsync()
    {
        if (_environment.IsDevelopment())
            return "80";

        var result = await RunCommandAsync($"{_ctl} port");
        return result.Trim();
    }

    public async Task ApplyDashboardSettingsAsync(Port port, RouteSalt salt)
    {
        if (_environment.IsDevelopment())
            return;

        await RunCommandAsync($"{_ctl} dashboard {port} {salt}");
    }

    private Task<string> RunCommandAsync(string command)
    {
        return Task.Run(() =>
        {
            lock (_connectLock)
            {
                if (!_client!.IsConnected)
                    _client.Connect();
            }

            var cmd = _client.RunCommand(command);

            if (cmd.ExitStatus != 0)
                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(cmd.Error) ? $"Command failed (exit {cmd.ExitStatus})" : cmd.Error.Trim());

            return cmd.Result;
        });
    }

    public void Dispose()
    {
        _client?.Dispose();
        GC.SuppressFinalize(this);
    }
}
