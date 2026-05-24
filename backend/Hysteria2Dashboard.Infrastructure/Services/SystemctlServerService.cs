using Hysteria2Dashboard.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Renci.SshNet;

namespace Hysteria2Dashboard.Infrastructure.Services;

public class SystemctlServerService : IHysteriaService, IDisposable
{
    private readonly SshClient _client;

    public SystemctlServerService(IConfiguration config)
    {
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
        await RunCommandAsync("sudo systemctl restart hysteria-server");
    }

    public async Task<string> GetStatusAsync()
    {
        var output = await RunCommandAsync("sudo systemctl is-active hysteria-server");
        return output.Trim();
    }

    private Task<string> RunCommandAsync(string command)
    {
        return Task.Run(() =>
        {
            if (!_client.IsConnected)
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
        _client.Dispose();
        GC.SuppressFinalize(this);
    }
}