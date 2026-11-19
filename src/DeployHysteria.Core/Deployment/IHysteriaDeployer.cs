using DeployHysteria.Core.Models.Deployment;

namespace DeployHysteria.Core.Deployment;

public interface IHysteriaDeployer
{
    Task<DeploymentResult> DeployAsync(
        DeployOptions options,
        Action<string>? progressCallback = null,
        CancellationToken cancellationToken = default);
}
