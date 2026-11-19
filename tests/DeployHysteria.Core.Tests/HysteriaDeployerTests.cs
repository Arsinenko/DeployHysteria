using DeployHysteria.Core.Deployment;
using DeployHysteria.Core.Models.Connection;
using DeployHysteria.Core.Models.Deployment;
using DeployHysteria.Core.Models.Hysteria;
using DeployHysteria.Core.Network;
using DeployHysteria.Core.Provisioning;
using DeployHysteria.Core.Serialization;
using DeployHysteria.Core.Ssh;
using FluentAssertions;
using Moq;
using Xunit;

namespace DeployHysteria.Core.Tests;

public class HysteriaDeployerTests
{
    private readonly Mock<ISshClientFactory> _mockSshFactory = new();
    private readonly Mock<ISshClient> _mockSsh = new();
    private readonly Mock<IDnsResolver> _mockDns = new();
    private readonly Mock<IPortChecker> _mockPortChecker = new();
    private readonly Mock<IServerProvisioner> _mockProvisioner = new();
    private readonly HysteriaConfigSerializer _configSerializer = new();
    private readonly DockerComposeGenerator _dockerComposeGenerator = new();

    private readonly HysteriaDeployer _deployer;

    public HysteriaDeployerTests()
    {
        _mockSshFactory.Setup(f => f.Create(It.IsAny<ServerConnectionInfo>()))
            .Returns(_mockSsh.Object);

        _mockSsh.Setup(s => s.ExecuteCommandAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandExecutionResult(0, "container started successfully", ""));

        _mockDns.Setup(d => d.VerifyDomainPointsToIpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockPortChecker.Setup(p => p.CheckPortAvailabilityAsync(It.IsAny<ISshClient>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PortCheckResult(IsAvailable: true));

        _deployer = new HysteriaDeployer(
            _mockSshFactory.Object,
            _mockDns.Object,
            _mockPortChecker.Object,
            _mockProvisioner.Object,
            _configSerializer,
            _dockerComposeGenerator);
    }

    [Theory]
    [InlineData(":443", 443)]
    [InlineData("0.0.0.0:8443", 8443)]
    [InlineData("443", 443)]
    [InlineData("127.0.0.1:10443", 10443)]
    [InlineData("", 443)]
    public void ExtractPortFromListen_ShouldParseCorrectly(string listen, int expectedPort)
    {
        var port = HysteriaDeployer.ExtractPortFromListen(listen);
        port.Should().Be(expectedPort);
    }

    [Fact]
    public async Task DeployAsync_HappyPath_ShouldSucceed()
    {
        var options = new DeployOptions
        {
            ServerConnection = new ServerConnectionInfo
            {
                Host = "1.2.3.4",
                Username = "root",
                Password = "pwd"
            },
            HysteriaConfig = new HysteriaConfig
            {
                Listen = ":443",
                Acme = new AcmeConfig { Domains = ["vpn.test.com"], Email = "admin@test.com" },
                Auth = AuthConfig.FromSinglePassword("vpnpass")
            }
        };

        var result = await _deployer.DeployAsync(options);

        result.Success.Should().BeTrue();
        result.DnsVerified.Should().BeTrue();
        result.PortVerified.Should().BeTrue();
        result.ExecutedSteps.Should().Contain("Hysteria container started");

        _mockSsh.Verify(s => s.UploadTextAsync(
            It.Is<string>(p => p.EndsWith("config.yaml")),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockSsh.Verify(s => s.UploadTextAsync(
            It.Is<string>(p => p.EndsWith("docker-compose.yml")),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockProvisioner.Verify(p => p.ProvisionAsync(
            _mockSsh.Object,
            options,
            It.IsAny<Action<string>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeployAsync_WhenDnsFails_ShouldAbortImmediately()
    {
        _mockDns.Setup(d => d.VerifyDomainPointsToIpAsync("vpn.test.com", "1.2.3.4", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var options = new DeployOptions
        {
            ServerConnection = new ServerConnectionInfo
            {
                Host = "1.2.3.4",
                Username = "root",
                Password = "pwd"
            },
            HysteriaConfig = new HysteriaConfig
            {
                Listen = ":443",
                Acme = new AcmeConfig { Domains = ["vpn.test.com"], Email = "admin@test.com" },
                Auth = AuthConfig.FromSinglePassword("vpnpass")
            }
        };

        var result = await _deployer.DeployAsync(options);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("DNS verification failed");
        _mockSsh.Verify(s => s.ConnectAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeployAsync_WhenPortOccupied_ShouldAbortBeforeProvisioning()
    {
        _mockPortChecker.Setup(p => p.CheckPortAvailabilityAsync(It.IsAny<ISshClient>(), 443, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PortCheckResult(IsAvailable: false, OccupiedBy: "apache2 (pid 999)"));

        var options = new DeployOptions
        {
            ServerConnection = new ServerConnectionInfo
            {
                Host = "1.2.3.4",
                Username = "root",
                Password = "pwd"
            },
            HysteriaConfig = new HysteriaConfig
            {
                Listen = ":443",
                Acme = new AcmeConfig { Domains = ["vpn.test.com"], Email = "admin@test.com" },
                Auth = AuthConfig.FromSinglePassword("vpnpass")
            }
        };

        var result = await _deployer.DeployAsync(options);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Port 443 is already in use");
        _mockProvisioner.Verify(p => p.ProvisionAsync(
            It.IsAny<ISshClient>(),
            It.IsAny<DeployOptions>(),
            It.IsAny<Action<string>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
