using DeployHysteria.Core.Management;
using DeployHysteria.Core.Models.Connection;
using DeployHysteria.Core.Models.Deployment;
using DeployHysteria.Core.Models.Hysteria;
using DeployHysteria.UI.Services;
using DeployHysteria.UI.ViewModels;
using FluentAssertions;
using Moq;
using Xunit;

namespace DeployHysteria.UI.Tests;

public class ManageViewModelTests
{
    private readonly Mock<IHysteriaManager> _mockManager = new();
    private readonly Mock<IHysteriaClientConfigService> _mockConfigService = new();
    private readonly Mock<IClipboardService> _mockClipboard = new();
    private readonly Mock<IFileSaveService> _mockFileSave = new();
    private readonly LocalizationService _loc = new();

    private readonly ManageViewModel _vm;

    public ManageViewModelTests()
    {
        _vm = new ManageViewModel(
            _mockManager.Object,
            _mockConfigService.Object,
            _mockClipboard.Object,
            _mockFileSave.Object,
            _loc);
    }

    [Fact]
    public async Task ConnectAsync_WhenHostMissing_ShouldSetErrorMessage()
    {
        _vm.Host = "";
        await _vm.ConnectAsync();

        _vm.ErrorMessage.Should().Contain("Host");
        _vm.IsConnected.Should().BeFalse();
    }

    [Fact]
    public async Task ConnectAsync_Success_ShouldPopulateStatusAndUsers()
    {
        _vm.Host = "1.2.3.4";
        _vm.Password = "pwd";

        var config = new HysteriaConfig
        {
            Listen = ":443",
            Acme = new AcmeConfig { Domains = ["vpn.domain.com"], Email = "admin@domain.com" },
            Auth = new AuthConfig
            {
                Type = "userpass",
                Userpass = new Dictionary<string, string> { ["alice"] = "pass1" }
            }
        };

        _mockManager.Setup(m => m.GetCurrentConfigAsync(It.IsAny<ServerConnectionInfo>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _mockManager.Setup(m => m.GetStatusAsync(It.IsAny<ServerConnectionInfo>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ServerStatus
            {
                ContainerRunning = true,
                ContainerStatus = "Up 2 hours",
                UfwStatus = "Status: active",
                Fail2BanStatus = "sshd: active",
                RecentLogs = "server logs..."
            });

        _mockManager.Setup(m => m.GetUsersAsync(It.IsAny<ServerConnectionInfo>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new HysteriaUser("alice", "pass1")]);

        await _vm.ConnectAsync();

        _vm.IsConnected.Should().BeTrue();
        _vm.ContainerRunning.Should().BeTrue();
        _vm.Users.Should().ContainSingle();
        _vm.Users[0].Username.Should().Be("alice");
    }

    [Fact]
    public async Task AddUserAsync_ShouldCallManagerAndRefresh()
    {
        _vm.Host = "1.2.3.4";
        _vm.Password = "pwd";
        _vm.NewUsername = "bob";
        _vm.NewUserPassword = "secretbobpass";

        var config = new HysteriaConfig
        {
            Listen = ":443",
            Acme = new AcmeConfig { Domains = ["vpn.domain.com"], Email = "admin@domain.com" },
            Auth = new AuthConfig { Type = "userpass", Userpass = new Dictionary<string, string>() }
        };

        _mockManager.Setup(m => m.GetCurrentConfigAsync(It.IsAny<ServerConnectionInfo>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _mockManager.Setup(m => m.GetUsersAsync(It.IsAny<ServerConnectionInfo>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new HysteriaUser("bob", "secretbobpass")]);

        _mockManager.Setup(m => m.GetStatusAsync(It.IsAny<ServerConnectionInfo>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ServerStatus { ContainerRunning = true });

        await _vm.AddUserAsync();

        _mockManager.Verify(m => m.AddOrUpdateUserAsync(
            It.IsAny<ServerConnectionInfo>(),
            "bob",
            "secretbobpass",
            It.IsAny<string>(),
            true,
            It.IsAny<CancellationToken>()), Times.Once);

        _vm.StatusMessage.Should().Contain("bob");
        _vm.Users.Should().ContainSingle();
    }
}
