using DeployHysteria.Core.Models.Hysteria;
using DeployHysteria.UI.Services;
using FluentAssertions;
using Xunit;

namespace DeployHysteria.UI.Tests;

public class HysteriaClientConfigServiceTests
{
    private readonly HysteriaClientConfigService _service = new();

    [Fact]
    public void GenerateUri_WithSinglePasswordAndObfs_ShouldProduceValidUri()
    {
        var info = new ClientConnectionInfo
        {
            ServerHost = "vpn.example.com",
            Port = 443,
            Username = "default",
            Password = "mypassword123",
            Sni = "vpn.example.com",
            ObfsType = "salamander",
            ObfsPassword = "obfspassword456",
            Insecure = false,
            Tag = "MyVPN"
        };

        var uri = _service.GenerateUri(info);

        uri.Should().StartWith("hysteria2://mypassword123@vpn.example.com:443/?");
        uri.Should().Contain("sni=vpn.example.com");
        uri.Should().Contain("obfs=salamander");
        uri.Should().Contain("obfs-password=obfspassword456");
        uri.Should().EndWith("#MyVPN");
        uri.Should().NotContain("insecure=1");
    }

    [Fact]
    public void GenerateUri_WithMultiUser_ShouldIncludeUsernameInUserInfo()
    {
        var info = new ClientConnectionInfo
        {
            ServerHost = "node1.domain.com",
            Port = 8443,
            Username = "alice",
            Password = "secret-pass",
            Sni = "node1.domain.com",
            Insecure = false,
            Tag = "alice@node1"
        };

        var uri = _service.GenerateUri(info);

        uri.Should().StartWith("hysteria2://alice:secret-pass@node1.domain.com:8443/?");
        uri.Should().Contain("sni=node1.domain.com");
        uri.Should().NotContain("obfs=");
        uri.Should().EndWith("#alice%40node1");
    }

    [Fact]
    public void GenerateTextConfig_ShouldContainAllParameters()
    {
        var info = new ClientConnectionInfo
        {
            ServerHost = "vpn.test.org",
            Port = 443,
            Username = "bob",
            Password = "bobpass123",
            Sni = "vpn.test.org",
            ObfsType = "salamander",
            ObfsPassword = "salamanderpass"
        };

        var text = _service.GenerateTextConfig(info);

        text.Should().Contain("Server (Host):        vpn.test.org");
        text.Should().Contain("Port:                 443");
        text.Should().Contain("Protocol:             Hysteria 2 (UDP/QUIC)");
        text.Should().Contain("Username:             bob");
        text.Should().Contain("Password:             bobpass123");
        text.Should().Contain("Obfuscation:          salamander");
        text.Should().Contain("Obfuscation Password: salamanderpass");
        text.Should().Contain("hysteria2://bob:bobpass123@vpn.test.org:443/?");
    }

    [Fact]
    public void GenerateQrCodePng_ShouldReturnValidPngHeader()
    {
        const string text = "hysteria2://test@vpn.com:443/?sni=vpn.com#test";
        var pngBytes = _service.GenerateQrCodePng(text);

        pngBytes.Should().NotBeNullOrEmpty();
        pngBytes.Length.Should().BeGreaterThan(50);

        // PNG Magic Number: 0x89 0x50 0x4E 0x47 0x0D 0x0A 0x1A 0x0A
        pngBytes[0].Should().Be(0x89);
        pngBytes[1].Should().Be(0x50); // P
        pngBytes[2].Should().Be(0x4E); // N
        pngBytes[3].Should().Be(0x47); // G
    }

    [Fact]
    public void CreateFromConfig_ShouldExtractParametersAccurately()
    {
        var config = new HysteriaConfig
        {
            Listen = ":443",
            Acme = new AcmeConfig { Domains = ["myvpn.com"], Email = "admin@myvpn.com" },
            Obfs = new ObfsConfig { Type = "salamander", Salamander = new ObfsSalamanderConfig { Password = "obfspass" } },
            Auth = AuthConfig.FromSinglePassword("pwd")
        };

        var clientInfo = _service.CreateFromConfig(config, "alice", "alicepass");

        clientInfo.ServerHost.Should().Be("myvpn.com");
        clientInfo.Port.Should().Be(443);
        clientInfo.Username.Should().Be("alice");
        clientInfo.Password.Should().Be("alicepass");
        clientInfo.ObfsType.Should().Be("salamander");
        clientInfo.ObfsPassword.Should().Be("obfspass");
    }
}
