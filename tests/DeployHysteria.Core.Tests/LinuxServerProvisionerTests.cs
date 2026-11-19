using DeployHysteria.Core.Models.Connection;
using DeployHysteria.Core.Models.Deployment;
using DeployHysteria.Core.Provisioning;
using DeployHysteria.Core.Ssh;
using FluentAssertions;
using Moq;
using Xunit;

namespace DeployHysteria.Core.Tests;

public class LinuxServerProvisionerTests
{
    private readonly Mock<ISshClient> _mockSsh = new();
    private readonly LinuxServerProvisioner _provisioner = new();

    public LinuxServerProvisionerTests()
    {
        _mockSsh.Setup(s => s.ExecuteCommandAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandExecutionResult(0, "success", ""));
    }

    [Fact]
    public async Task UpdateSystemPackages_ShouldRunAptCommands()
    {
        await _provisioner.UpdateSystemPackagesAsync(_mockSsh.Object, CancellationToken.None);

        _mockSsh.Verify(s => s.ExecuteCommandAsync(
            It.Is<string>(cmd => cmd.Contains("apt-get update") && cmd.Contains("apt-get upgrade")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfigureFirewall_ShouldOpenSshAndHysteriaPorts()
    {
        var firewall = new FirewallOptions
        {
            Enabled = true,
            SshPort = 2222,
            HysteriaPort = 443
        };

        await _provisioner.ConfigureFirewallAsync(_mockSsh.Object, firewall, CancellationToken.None);

        _mockSsh.Verify(s => s.ExecuteCommandAsync(
            It.Is<string>(cmd => cmd == "ufw allow 2222/tcp"),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockSsh.Verify(s => s.ExecuteCommandAsync(
            It.Is<string>(cmd => cmd == "ufw allow 443/tcp"),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockSsh.Verify(s => s.ExecuteCommandAsync(
            It.Is<string>(cmd => cmd == "ufw allow 443/udp"),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockSsh.Verify(s => s.ExecuteCommandAsync(
            It.Is<string>(cmd => cmd == "ufw --force enable"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfigureFail2Ban_ShouldUploadConfigAndRestartService()
    {
        var fail2ban = new Fail2BanOptions
        {
            Enabled = true,
            MaxRetry = 3,
            BanTime = TimeSpan.FromHours(10),
            FindTime = TimeSpan.FromMinutes(10),
            SshPort = 22
        };

        string uploadedContent = "";
        _mockSsh.Setup(s => s.UploadTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((_, content, _) => uploadedContent = content)
            .Returns(Task.CompletedTask);

        await _provisioner.ConfigureFail2BanAsync(_mockSsh.Object, fail2ban, CancellationToken.None);

        uploadedContent.Should().Contain("maxretry = 3");
        // 10 hours = 36000 seconds
        uploadedContent.Should().Contain("bantime = 36000");
        uploadedContent.Should().Contain("findtime = 600");
        uploadedContent.Should().Contain("port = 22");

        _mockSsh.Verify(s => s.ExecuteCommandAsync(
            It.Is<string>(cmd => cmd.Contains("systemctl restart fail2ban")),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
