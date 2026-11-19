using DeployHysteria.Core.Security;
using DeployHysteria.Core.Ssh;

namespace DeployHysteria.Core.Network;

public class RemotePortChecker : IPortChecker
{
    public async Task<PortCheckResult> CheckPortAvailabilityAsync(ISshClient sshClient, int port, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sshClient);
        if (port is < 1 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(port), "Port must be between 1 and 65535.");
        }

        // We check using ss or netstat or lsof
        // Check both TCP and UDP listening sockets
        var escapedPort = ShellSanitizer.Escape($":{port}");
        var cmd = $"ss -tulpn | grep {escapedPort} || true";

        var result = await sshClient.ExecuteCommandAsync(cmd, cancellationToken);
        var output = result.StandardOutput.Trim();

        if (string.IsNullOrWhiteSpace(output))
        {
            return new PortCheckResult(IsAvailable: true);
        }

        return new PortCheckResult(IsAvailable: false, OccupiedBy: output);
    }
}
