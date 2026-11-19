using DeployHysteria.Core.Models.Connection;
using DeployHysteria.Core.Models.Deployment;
using DeployHysteria.Core.Models.Hysteria;
using DeployHysteria.Core.Security;
using DeployHysteria.Core.Serialization;
using DeployHysteria.Core.Ssh;

namespace DeployHysteria.Core.Management;

public class HysteriaManager : IHysteriaManager
{
    private readonly ISshClientFactory _sshClientFactory;
    private readonly IHysteriaConfigSerializer _configSerializer;

    public HysteriaManager(
        ISshClientFactory sshClientFactory,
        IHysteriaConfigSerializer configSerializer)
    {
        _sshClientFactory = sshClientFactory ?? throw new ArgumentNullException(nameof(sshClientFactory));
        _configSerializer = configSerializer ?? throw new ArgumentNullException(nameof(configSerializer));
    }

    public async Task<HysteriaConfig> GetCurrentConfigAsync(
        ServerConnectionInfo connection,
        string remoteDir = "/opt/hysteria2",
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        connection.Validate();

        await using var client = _sshClientFactory.Create(connection);
        await client.ConnectAsync(cancellationToken);

        var configPath = $"{remoteDir.TrimEnd('/')}/config.yaml";
        var exists = await client.FileExistsAsync(configPath, cancellationToken);
        if (!exists)
        {
            throw new FileNotFoundException($"Hysteria config file not found at: {configPath}");
        }

        var yaml = await client.DownloadTextAsync(configPath, cancellationToken);
        return _configSerializer.Deserialize(yaml);
    }

    public async Task UpdateConfigAsync(
        ServerConnectionInfo connection,
        HysteriaConfig updatedConfig,
        string remoteDir = "/opt/hysteria2",
        bool restartContainer = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(updatedConfig);
        connection.Validate();
        updatedConfig.Validate();

        await using var client = _sshClientFactory.Create(connection);
        await client.ConnectAsync(cancellationToken);

        var configPath = $"{remoteDir.TrimEnd('/')}/config.yaml";
        var yaml = _configSerializer.Serialize(updatedConfig);
        await client.UploadTextAsync(configPath, yaml, cancellationToken);

        if (restartContainer)
        {
            var escapedDir = ShellSanitizer.Escape(remoteDir);
            var restartCmd = $"cd {escapedDir} && (docker compose restart || docker-compose restart)";
            var restartRes = await client.ExecuteCommandAsync(restartCmd, cancellationToken);
            restartRes.EnsureSuccess("Failed to restart Hysteria container after config update");
        }
    }

    public async Task<IReadOnlyList<HysteriaUser>> GetUsersAsync(
        ServerConnectionInfo connection,
        string remoteDir = "/opt/hysteria2",
        CancellationToken cancellationToken = default)
    {
        var config = await GetCurrentConfigAsync(connection, remoteDir, cancellationToken);
        return config.GetUsers();
    }

    public async Task AddOrUpdateUserAsync(
        ServerConnectionInfo connection,
        string username,
        string password,
        string remoteDir = "/opt/hysteria2",
        bool restartContainer = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var config = await GetCurrentConfigAsync(connection, remoteDir, cancellationToken);
        config.AddOrUpdateUser(username, password);

        await UpdateConfigAsync(connection, config, remoteDir, restartContainer, cancellationToken);
    }

    public async Task<bool> RemoveUserAsync(
        ServerConnectionInfo connection,
        string username,
        string remoteDir = "/opt/hysteria2",
        bool restartContainer = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);

        var config = await GetCurrentConfigAsync(connection, remoteDir, cancellationToken);
        var removed = config.RemoveUser(username);

        if (removed)
        {
            await UpdateConfigAsync(connection, config, remoteDir, restartContainer, cancellationToken);
        }

        return removed;
    }

    public async Task<ServerStatus> GetStatusAsync(
        ServerConnectionInfo connection,
        string remoteDir = "/opt/hysteria2",
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        connection.Validate();

        await using var client = _sshClientFactory.Create(connection);
        await client.ConnectAsync(cancellationToken);

        var escapedDir = ShellSanitizer.Escape(remoteDir);

        // Container status
        var psCmd = $"cd {escapedDir} && (docker compose ps --format json || docker compose ps || docker-compose ps || true)";
        var psRes = await client.ExecuteCommandAsync(psCmd, cancellationToken);

        var inspectCmd = "docker inspect -f '{{.State.Running}}' hysteria2 2>/dev/null || echo 'false'";
        var inspectRes = await client.ExecuteCommandAsync(inspectCmd, cancellationToken);
        var isRunning = inspectRes.StandardOutput.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);

        // UFW status
        var ufwRes = await client.ExecuteCommandAsync("ufw status verbose 2>/dev/null || true", cancellationToken);

        // Fail2Ban status
        var f2bRes = await client.ExecuteCommandAsync("fail2ban-client status sshd 2>/dev/null || fail2ban-client status 2>/dev/null || true", cancellationToken);

        // Recent logs
        var logsCmd = $"cd {escapedDir} && (docker compose logs --tail=20 || docker-compose logs --tail=20 || true)";
        var logsRes = await client.ExecuteCommandAsync(logsCmd, cancellationToken);

        return new ServerStatus
        {
            ContainerRunning = isRunning,
            ContainerStatus = psRes.StandardOutput.Trim(),
            UfwStatus = ufwRes.StandardOutput.Trim(),
            Fail2BanStatus = f2bRes.StandardOutput.Trim(),
            RecentLogs = logsRes.StandardOutput.Trim() + logsRes.StandardError.Trim()
        };
    }

    public async Task<string> GetLogsAsync(
        ServerConnectionInfo connection,
        int lines = 100,
        string remoteDir = "/opt/hysteria2",
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        connection.Validate();

        if (lines < 1) lines = 100;

        await using var client = _sshClientFactory.Create(connection);
        await client.ConnectAsync(cancellationToken);

        var escapedDir = ShellSanitizer.Escape(remoteDir);
        var logsCmd = $"cd {escapedDir} && (docker compose logs --tail={lines} || docker-compose logs --tail={lines})";
        var logsRes = await client.ExecuteCommandAsync(logsCmd, cancellationToken);

        return logsRes.StandardOutput + logsRes.StandardError;
    }

    public async Task RestartServiceAsync(
        ServerConnectionInfo connection,
        string remoteDir = "/opt/hysteria2",
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        connection.Validate();

        await using var client = _sshClientFactory.Create(connection);
        await client.ConnectAsync(cancellationToken);

        var escapedDir = ShellSanitizer.Escape(remoteDir);
        var restartCmd = $"cd {escapedDir} && (docker compose restart || docker-compose restart)";
        var restartRes = await client.ExecuteCommandAsync(restartCmd, cancellationToken);
        restartRes.EnsureSuccess("Failed to restart Hysteria service");
    }
}
