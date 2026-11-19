using DeployHysteria.Core.Management;
using DeployHysteria.Core.Models.Connection;
using DeployHysteria.Core.Models.Hysteria;
using DeployHysteria.Core.Serialization;
using DeployHysteria.Core.Ssh;
using FluentAssertions;
using Moq;
using Xunit;

namespace DeployHysteria.Core.Tests;

public class HysteriaManagerTests
{
    private readonly Mock<ISshClientFactory> _mockSshFactory = new();
    private readonly Mock<ISshClient> _mockSsh = new();
    private readonly HysteriaConfigSerializer _serializer = new();
    private readonly HysteriaManager _manager;

    private readonly ServerConnectionInfo _connection = new()
    {
        Host = "1.2.3.4",
        Username = "root",
        Password = "pwd"
    };

    public HysteriaManagerTests()
    {
        _mockSshFactory.Setup(f => f.Create(It.IsAny<ServerConnectionInfo>()))
            .Returns(_mockSsh.Object);

        _mockSsh.Setup(s => s.ExecuteCommandAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandExecutionResult(0, "ok", ""));

        _manager = new HysteriaManager(_mockSshFactory.Object, _serializer);
    }

    [Fact]
    public async Task GetCurrentConfigAsync_WhenFileExists_ShouldReturnParsedConfig()
    {
        const string mockYaml = """
            listen: :443
            acme:
              domains:
                - vpn.example.com
              email: admin@example.com
            auth:
              type: userpass
              userpass:
                alice: pass1
                bob: pass2
            """;

        _mockSsh.Setup(s => s.FileExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockSsh.Setup(s => s.DownloadTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockYaml);

        var config = await _manager.GetCurrentConfigAsync(_connection);

        config.Listen.Should().Be(":443");
        config.Acme.Domains.Should().ContainSingle().Which.Should().Be("vpn.example.com");
        config.Auth.Type.Should().Be("userpass");
        config.Auth.Userpass.Should().HaveCount(2);
    }

    [Fact]
    public async Task AddOrUpdateUserAsync_ShouldUpdateConfigAndRestartService()
    {
        const string initialYaml = """
            listen: :443
            acme:
              domains:
                - vpn.example.com
              email: admin@example.com
            auth:
              type: userpass
              userpass:
                alice: pass1
            """;

        _mockSsh.Setup(s => s.FileExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockSsh.Setup(s => s.DownloadTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(initialYaml);

        string uploadedYaml = "";
        _mockSsh.Setup(s => s.UploadTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((_, text, _) => uploadedYaml = text)
            .Returns(Task.CompletedTask);

        await _manager.AddOrUpdateUserAsync(_connection, "charlie", "pass3");

        uploadedYaml.Should().Contain("charlie: pass3");
        uploadedYaml.Should().Contain("alice: pass1");

        _mockSsh.Verify(s => s.ExecuteCommandAsync(
            It.Is<string>(cmd => cmd.Contains("docker compose restart")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveUserAsync_ExistingUser_ShouldRemoveAndRestart()
    {
        const string initialYaml = """
            listen: :443
            acme:
              domains:
                - vpn.example.com
              email: admin@example.com
            auth:
              type: userpass
              userpass:
                alice: pass1
                bob: pass2
            """;

        _mockSsh.Setup(s => s.FileExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockSsh.Setup(s => s.DownloadTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(initialYaml);

        string uploadedYaml = "";
        _mockSsh.Setup(s => s.UploadTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((_, text, _) => uploadedYaml = text)
            .Returns(Task.CompletedTask);

        var result = await _manager.RemoveUserAsync(_connection, "alice");

        result.Should().BeTrue();
        uploadedYaml.Should().NotContain("alice");
        uploadedYaml.Should().Contain("bob: pass2");

        _mockSsh.Verify(s => s.ExecuteCommandAsync(
            It.Is<string>(cmd => cmd.Contains("docker compose restart")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetStatusAsync_ShouldReturnAggregatedStatus()
    {
        _mockSsh.Setup(s => s.ExecuteCommandAsync(It.Is<string>(c => c.Contains("docker inspect")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandExecutionResult(0, "true\n", ""));
        _mockSsh.Setup(s => s.ExecuteCommandAsync(It.Is<string>(c => c.Contains("ufw status")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandExecutionResult(0, "Status: active\n443 ALLOW", ""));
        _mockSsh.Setup(s => s.ExecuteCommandAsync(It.Is<string>(c => c.Contains("fail2ban-client status")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandExecutionResult(0, "Jail: sshd, Total banned: 0", ""));
        _mockSsh.Setup(s => s.ExecuteCommandAsync(It.Is<string>(c => c.Contains("docker compose logs")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandExecutionResult(0, "hysteria2 listening on :443", ""));

        var status = await _manager.GetStatusAsync(_connection);

        status.ContainerRunning.Should().BeTrue();
        status.UfwStatus.Should().Contain("Status: active");
        status.Fail2BanStatus.Should().Contain("Jail: sshd");
        status.RecentLogs.Should().Contain("listening on :443");
    }
}
