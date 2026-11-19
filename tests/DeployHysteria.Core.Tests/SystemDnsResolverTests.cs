using DeployHysteria.Core.Network;
using FluentAssertions;
using Xunit;

namespace DeployHysteria.Core.Tests;

public class SystemDnsResolverTests
{
    private readonly SystemDnsResolver _resolver = new();

    [Fact]
    public async Task VerifyDomainPointsToIpAsync_InvalidDomain_ShouldThrow()
    {
        var act = async () => await _resolver.VerifyDomainPointsToIpAsync("", "1.2.3.4");
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task VerifyDomainPointsToIpAsync_InvalidIp_ShouldThrow()
    {
        var act = async () => await _resolver.VerifyDomainPointsToIpAsync("example.com", "");
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ResolveHostAsync_EmptyHost_ShouldThrow()
    {
        var act = async () => await _resolver.ResolveHostAsync("");
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
