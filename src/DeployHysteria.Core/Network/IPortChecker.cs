using DeployHysteria.Core.Ssh;

namespace DeployHysteria.Core.Network;

public record PortCheckResult(bool IsAvailable, string? OccupiedBy = null);

public interface IPortChecker
{
    Task<PortCheckResult> CheckPortAvailabilityAsync(ISshClient sshClient, int port, CancellationToken cancellationToken = default);
}
