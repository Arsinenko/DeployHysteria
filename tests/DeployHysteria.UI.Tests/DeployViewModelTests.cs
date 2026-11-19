using DeployHysteria.Core.Deployment;
using DeployHysteria.Core.Models.Deployment;
using DeployHysteria.UI.Services;
using DeployHysteria.UI.ViewModels;
using FluentAssertions;
using Moq;
using Xunit;

namespace DeployHysteria.UI.Tests;

public class DeployViewModelTests
{
    private readonly Mock<IHysteriaDeployer> _mockDeployer = new();
    private readonly Mock<IHysteriaClientConfigService> _mockConfigService = new();
    private readonly Mock<IClipboardService> _mockClipboard = new();
    private readonly Mock<IFileSaveService> _mockFileSave = new();
    private readonly LocalizationService _loc = new();

    private readonly DeployViewModel _vm;

    public DeployViewModelTests()
    {
        _mockConfigService.Setup(c => c.GenerateQrCodePng(It.IsAny<string>(), It.IsAny<int>()))
            .Returns([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

        _vm = new DeployViewModel(
            _mockDeployer.Object,
            _mockConfigService.Object,
            _mockClipboard.Object,
            _mockFileSave.Object,
            _loc);
    }

    [Fact]
    public void GeneratePasswords_ShouldSetClientAndObfsPasswords()
    {
        _vm.GeneratePasswords();

        _vm.ClientPassword.Should().NotBeNullOrWhiteSpace();
        _vm.ObfsPassword.Should().NotBeNullOrWhiteSpace();
        _vm.ClientPassword.Length.Should().Be(18);
    }

    [Fact]
    public void ToggleMode_ShouldSwitchBetweenSimpleAndAdvanced()
    {
        _vm.IsAdvancedMode.Should().BeFalse();
        _vm.ToggleMode();
        _vm.IsAdvancedMode.Should().BeTrue();
        _vm.ToggleMode();
        _vm.IsAdvancedMode.Should().BeFalse();
    }

    [Fact]
    public async Task StartDeploy_WhenHostEmpty_ShouldSetErrorMessage()
    {
        _vm.Host = "";
        await _vm.StartDeployAsync();

        _vm.ErrorMessage.Should().Contain("Host");
        _mockDeployer.Verify(d => d.DeployAsync(It.IsAny<DeployOptions>(), It.IsAny<Action<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task StartDeploy_HappyPath_ShouldSucceed()
    {
        _vm.Host = "1.2.3.4";
        _vm.Domain = "vpn.test.com";
        _vm.Email = "admin@test.com";
        _vm.Password = "rootpass";
        _vm.ClientPassword = "clientpass";

        _mockDeployer.Setup(d => d.DeployAsync(It.IsAny<DeployOptions>(), It.IsAny<Action<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(DeploymentResult.Ok("Deployed", "logs", true, true, ["step1"]));

        _mockConfigService.Setup(c => c.CreateFromConfig(It.IsAny<DeployHysteria.Core.Models.Hysteria.HysteriaConfig>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new ClientConnectionInfo
            {
                ServerHost = "vpn.test.com",
                Password = "clientpass"
            });

        _mockConfigService.Setup(c => c.GenerateUri(It.IsAny<ClientConnectionInfo>()))
            .Returns("hysteria2://clientpass@vpn.test.com:443/?sni=vpn.test.com#test");

        _mockConfigService.Setup(c => c.GenerateTextConfig(It.IsAny<ClientConnectionInfo>()))
            .Returns("text config");

        // Real tiny 1x1 PNG bytes for Avalonia Bitmap decoder
        byte[] tinyPng =
        [
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D,
            0x49, 0x48, 0x44, 0x52, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
            0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4, 0x89, 0x00, 0x00, 0x00,
            0x0A, 0x49, 0x44, 0x41, 0x54, 0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00,
            0x05, 0x00, 0x01, 0x0D, 0x0A, 0x2D, 0xB4, 0x00, 0x00, 0x00, 0x00, 0x49,
            0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82
        ];

        _mockConfigService.Setup(c => c.GenerateQrCodePng(It.IsAny<string>(), It.IsAny<int>()))
            .Returns(tinyPng);

        await _vm.StartDeployAsync();

        _vm.IsSuccess.Should().BeTrue();
        _vm.StatusMessage.Should().Contain("successfully");
        _vm.QrDialog.Should().NotBeNull();
        _vm.QrDialog!.IsOpen.Should().BeTrue();
    }
}
