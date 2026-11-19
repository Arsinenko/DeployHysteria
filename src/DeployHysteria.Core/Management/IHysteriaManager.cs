using DeployHysteria.Core.Models.Connection;
using DeployHysteria.Core.Models.Deployment;
using DeployHysteria.Core.Models.Hysteria;

namespace DeployHysteria.Core.Management;

public interface IHysteriaManager
{
    Task<HysteriaConfig> GetCurrentConfigAsync(
        ServerConnectionInfo connection,
        string remoteDir = "/opt/hysteria2",
        CancellationToken cancellationToken = default);

    Task UpdateConfigAsync(
        ServerConnectionInfo connection,
        HysteriaConfig updatedConfig,
        string remoteDir = "/opt/hysteria2",
        bool restartContainer = true,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HysteriaUser>> GetUsersAsync(
        ServerConnectionInfo connection,
        string remoteDir = "/opt/hysteria2",
        CancellationToken cancellationToken = default);

    Task AddOrUpdateUserAsync(
        ServerConnectionInfo connection,
        string username,
        string password,
        string remoteDir = "/opt/hysteria2",
        bool restartContainer = true,
        CancellationToken cancellationToken = default);

    Task<bool> RemoveUserAsync(
        ServerConnectionInfo connection,
        string username,
        string remoteDir = "/opt/hysteria2",
        bool restartContainer = true,
        CancellationToken cancellationToken = default);

    Task<ServerStatus> GetStatusAsync(
        ServerConnectionInfo connection,
        string remoteDir = "/opt/hysteria2",
        CancellationToken cancellationToken = default);

    Task<string> GetLogsAsync(
        ServerConnectionInfo connection,
        int lines = 100,
        string remoteDir = "/opt/hysteria2",
        CancellationToken cancellationToken = default);

    Task RestartServiceAsync(
        ServerConnectionInfo connection,
        string remoteDir = "/opt/hysteria2",
        CancellationToken cancellationToken = default);
}
