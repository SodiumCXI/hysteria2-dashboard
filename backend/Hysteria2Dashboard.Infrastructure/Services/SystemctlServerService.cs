using Hysteria2Dashboard.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Renci.SshNet;

namespace Hysteria2Dashboard.Infrastructure.Services;

public class SystemctlServerService : IHysteriaService, IDisposable
{
    private readonly SshClient? _client;
    private readonly IHostEnvironment _environment;

    public SystemctlServerService(IConfiguration config, IHostEnvironment environment)
    {
        _environment = environment;

        if (_environment.IsDevelopment())
            return;

        var host = config["SSH:Host"] ?? "host.docker.internal";
        var user = config["SSH:User"] ?? "hysteria";
        var keyPath = config["SSH:KeyPath"] ?? "/app/ssh_key";
        var port = int.Parse(config["SSH:Port"] ?? "22");

        var keyFile = new PrivateKeyFile(keyPath);
        var authMethod = new PrivateKeyAuthenticationMethod(user, keyFile);
        var connInfo = new ConnectionInfo(host, port, user, authMethod);

        _client = new SshClient(connInfo);
    }

    public async Task RestartAsync()
    {
        if (_environment.IsDevelopment())
            return;

        await RunCommandAsync("sudo systemctl restart hysteria-server");
    }

    public async Task<string> GetRawStatusAsync()
    {
        if (_environment.IsDevelopment())
        {
            string[] statuses = ["active", "inactive", "activating"];
            return statuses[Random.Shared.Next(statuses.Length)];
        }

        var output = await RunCommandAsync("sudo systemctl is-active hysteria-server");
        return output.Trim();
    }

    private Task<string> RunCommandAsync(string command)
    {
        return Task.Run(() =>
        {
            if (!_client!.IsConnected)
                _client.Connect();

            var cmd = _client.RunCommand(command);

            if (cmd.ExitStatus != 0 && cmd.ExitStatus != 3 && !string.IsNullOrEmpty(cmd.Error))
                throw new InvalidOperationException(
                    $"Command failed (exit {cmd.ExitStatus}): {cmd.Error}");

            return cmd.Result;
        });
    }

    public void Dispose()
    {
        _client?.Dispose();
        GC.SuppressFinalize(this);
    }
}
