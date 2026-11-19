using DeployHysteria.Core.Models.Deployment;
using DeployHysteria.Core.Ssh;

namespace DeployHysteria.Core.Provisioning;

public interface IServerProvisioner
{
    Task ProvisionAsync(
        ISshClient sshClient,
        DeployOptions options,
        Action<string>? progressCallback = null,
        CancellationToken cancellationToken = default);
}
