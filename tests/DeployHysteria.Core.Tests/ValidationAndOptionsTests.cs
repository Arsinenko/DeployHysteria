using DeployHysteria.Core.Models.Connection;
using DeployHysteria.Core.Models.Deployment;
using DeployHysteria.Core.Models.Hysteria;
using FluentAssertions;
using Xunit;

namespace DeployHysteria.Core.Tests;

public class ValidationAndOptionsTests
{
    [Fact]
    public void ServerConnectionInfo_ValidWithPassword_ShouldPassValidation()
    {
        var info = new ServerConnectionInfo
        {
            Host = "1.2.3.4",
            Port = 22,
            Username = "root",
            Password = "secret-password"
        };

        var act = () => info.Validate();
        act.Should().NotThrow();
    }

    [Fact]
    public void ServerConnectionInfo_ValidWithKey_ShouldPassValidation()
    {
        var info = new ServerConnectionInfo
        {
            Host = "1.2.3.4",
            Port = 2222,
            Username = "ubuntu",
            SshPrivateKey = "-----BEGIN OPENSSH PRIVATE KEY-----\n..."
        };

        var act = () => info.Validate();
        act.Should().NotThrow();
    }

    [Fact]
    public void ServerConnectionInfo_WithoutHost_ShouldThrow()
    {
        var info = new ServerConnectionInfo
        {
            Host = "",
            Username = "root",
            Password = "password"
        };

        var act = () => info.Validate();
        act.Should().Throw<ArgumentException>().WithParameterName("Host");
    }

    [Fact]
    public void ServerConnectionInfo_InvalidPort_ShouldThrow()
    {
        var info = new ServerConnectionInfo
        {
            Host = "1.2.3.4",
            Port = 70000,
            Username = "root",
            Password = "password"
        };

        var act = () => info.Validate();
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("Port");
    }

    [Fact]
    public void ServerConnectionInfo_WithoutCredentials_ShouldThrow()
    {
        var info = new ServerConnectionInfo
        {
            Host = "1.2.3.4",
            Username = "root"
        };

        var act = () => info.Validate();
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Fail2BanOptions_Valid_ShouldPassValidation()
    {
        var options = new Fail2BanOptions
        {
            Enabled = true,
            MaxRetry = 3,
            BanTime = TimeSpan.FromHours(10),
            FindTime = TimeSpan.FromMinutes(10),
            SshPort = 22
        };

        var act = () => options.Validate();
        act.Should().NotThrow();
    }

    [Fact]
    public void Fail2BanOptions_InvalidMaxRetry_ShouldThrow()
    {
        var options = new Fail2BanOptions { MaxRetry = 0 };
        var act = () => options.Validate();
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void FirewallOptions_InvalidHysteriaPort_ShouldThrow()
    {
        var options = new FirewallOptions { HysteriaPort = 0 };
        var act = () => options.Validate();
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void HysteriaConfig_MissingAcmeDomain_ShouldThrow()
    {
        var config = new HysteriaConfig
        {
            Listen = ":443",
            Acme = new AcmeConfig { Domains = [], Email = "admin@example.com" },
            Auth = AuthConfig.FromSinglePassword("secret")
        };

        var act = () => config.Validate();
        act.Should().Throw<InvalidOperationException>().WithMessage("*ACME domains*");
    }

    [Fact]
    public void HysteriaConfig_SalamanderWithoutPassword_ShouldThrow()
    {
        var config = new HysteriaConfig
        {
            Listen = ":443",
            Acme = new AcmeConfig { Domains = ["example.com"], Email = "admin@example.com" },
            Auth = AuthConfig.FromSinglePassword("secret"),
            Obfs = new ObfsConfig { Type = "salamander", Salamander = new ObfsSalamanderConfig { Password = "" } }
        };

        var act = () => config.Validate();
        act.Should().Throw<InvalidOperationException>().WithMessage("*password*");
    }
}
