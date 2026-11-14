using Hysteria2Dashboard.Domain.Entities;
using Hysteria2Dashboard.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Hysteria2Dashboard.Infrastructure.Repositories;

public class YamlConfigRepository(IConfiguration configuration) : IUserRepository, IHysteriaSettingsStore
{
    private static readonly string[] DefaultAcl =
    [
        "reject(0.0.0.0/8)",
        "reject(10.0.0.0/8)",
        "reject(100.64.0.0/10)",
        "reject(127.0.0.0/8)",
        "reject(169.254.0.0/16)",
        "reject(172.16.0.0/12)",
        "reject(192.168.0.0/16)",
        "reject(::/128)",
        "reject(::1/128)",
        "reject(fc00::/7)",
        "reject(fe80::/10)",
        "reject(all, tcp/25)"
    ];

    private readonly string _configPath = configuration["Hysteria2:ConfigPath"] ?? "/etc/hysteria/config.yaml";

    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task<List<User>> GetAllUsersAsync()
    {
        var users = Users(Root(await ReadConfig()));

        return [.. users.Children.Select(kv => new User(Text(kv.Key), Text(kv.Value)))];
    }

    public async Task AddUserAsync(User user)
    {
        await _lock.WaitAsync();
        try
        {
            var config = await ReadConfig();
            Users(Root(config)).Children[Quoted(user.Username)] = Quoted(user.Password);
            await WriteConfig(config);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> ExistsAsync(string username)
    {
        var users = await GetAllUsersAsync();
        return users.Any(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));
    }

    public async Task DeleteUserAsync(string username)
    {
        await _lock.WaitAsync();
        try
        {
            var config = await ReadConfig();
            var users = Users(Root(config));

            var keys = users.Children.Keys
                .Where(k => string.Equals(Text(k), username, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var key in keys)
                users.Children.Remove(key);

            await WriteConfig(config);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<HysteriaSettings> GetHysteriaSettingsAsync()
    {
        var root = Root(await ReadConfig());

        var listen = Text(Child(root, "listen"));
        var port = string.IsNullOrEmpty(listen) ? "443" : listen[(listen.LastIndexOf(':') + 1)..];

        return new HysteriaSettings(port, Text(Child(Section(Section(root, "obfs"), "salamander"), "password")));
    }

    public async Task SaveHysteriaSettingsAsync(HysteriaSettings settings)
    {
        await _lock.WaitAsync();

        try
        {
            var config = await ReadConfig();
            var root = Root(config);

            root.Children[Key("listen")] = new YamlScalarNode($":{settings.Port}");

            var obfs = Section(root, "obfs");
            obfs.Children[Key("type")] = new YamlScalarNode("salamander");
            Section(obfs, "salamander").Children[Key("password")] = Quoted(settings.ObfsPassword);

            await WriteConfig(config);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<string?> GetLegacySniAsync()
    {
        if (!File.Exists(_configPath))
            return null;

        var root = Root(await ReadConfig());

        if (!root.Children.ContainsKey(Key("masquerade")))
            return null;

        var url = Text(Child(Section(Section(root, "masquerade"), "proxy"), "url"));
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.Host : null;
    }

    public async Task NormalizeAsync()
    {
        if (!File.Exists(_configPath))
            return;

        await _lock.WaitAsync();

        try
        {
            var config = await ReadConfig();
            var root = Root(config);

            var changed = root.Children.Remove(Key("masquerade"));
            changed |= root.Children.Remove(Key("bandwidth"));

            if (Text(Child(root, "ignoreClientBandwidth")) != "true")
            {
                root.Children[Key("ignoreClientBandwidth")] = new YamlScalarNode("true");
                changed = true;
            }

            if (!root.Children.ContainsKey(Key("acl")))
            {
                Section(root, "acl").Children[Key("inline")] = new YamlSequenceNode(DefaultAcl.Select(rule => new YamlScalarNode(rule)));
                changed = true;
            }

            if (changed)
                await WriteConfig(config);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<YamlStream> ReadConfig()
    {
        var config = new YamlStream();
        config.Load(new StringReader(await File.ReadAllTextAsync(_configPath)));
        return config;
    }

    private async Task WriteConfig(YamlStream config)
    {
        var backupPath = _configPath + ".bak";
        if (!File.Exists(backupPath))
            File.Copy(_configPath, backupPath);

        using var writer = new StringWriter();
        config.Save(writer, assignAnchors: false);

        var yaml = writer.ToString().Replace("\r\n", "\n").TrimEnd();
        if (yaml.EndsWith("\n..."))
            yaml = yaml[..^3].TrimEnd();

        await File.WriteAllTextAsync(_configPath, yaml + "\n");
    }

    private static YamlMappingNode Root(YamlStream config)
        => config.Documents.Count > 0 && config.Documents[0].RootNode is YamlMappingNode root
            ? root
            : throw new InvalidOperationException("Hysteria2 config is empty or invalid");

    private static YamlMappingNode Users(YamlMappingNode root)
        => Section(Section(root, "auth"), "userpass");

    private static YamlMappingNode Section(YamlMappingNode parent, string name)
    {
        if (Child(parent, name) is YamlMappingNode section)
            return section;

        section = new YamlMappingNode();
        parent.Children[Key(name)] = section;
        return section;
    }

    private static YamlNode? Child(YamlMappingNode parent, string name)
        => parent.Children.TryGetValue(Key(name), out var node) ? node : null;

    private static string Text(YamlNode? node)
        => (node as YamlScalarNode)?.Value ?? string.Empty;

    private static YamlScalarNode Key(string name) => new(name);

    private static YamlScalarNode Quoted(string value) => new(value) { Style = ScalarStyle.DoubleQuoted };
}
