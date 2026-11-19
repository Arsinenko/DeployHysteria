using System.Net;
using System.Net.Sockets;

namespace DeployHysteria.Core.Network;

public class SystemDnsResolver : IDnsResolver
{
    public async Task<IReadOnlyList<IPAddress>> ResolveHostAsync(string host, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host);

        try
        {
            var addresses = await Dns.GetHostAddressesAsync(host, cancellationToken);
            return addresses;
        }
        catch (SocketException)
        {
            return [];
        }
    }

    public async Task<bool> VerifyDomainPointsToIpAsync(string domain, string expectedIp, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(domain);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedIp);

        if (!IPAddress.TryParse(expectedIp, out var parsedExpectedIp))
        {
            // If expectedIp was given as a hostname, resolve it too
            var targetAddresses = await ResolveHostAsync(expectedIp, cancellationToken);
            if (targetAddresses.Count == 0) return false;
            parsedExpectedIp = targetAddresses[0];
        }

        var resolvedAddresses = await ResolveHostAsync(domain, cancellationToken);
        return resolvedAddresses.Any(ip => ip.Equals(parsedExpectedIp));
    }
}
