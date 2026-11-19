using System.Net;

namespace DeployHysteria.Core.Network;

public interface IDnsResolver
{
    Task<IReadOnlyList<IPAddress>> ResolveHostAsync(string host, CancellationToken cancellationToken = default);
    Task<bool> VerifyDomainPointsToIpAsync(string domain, string expectedIp, CancellationToken cancellationToken = default);
}
