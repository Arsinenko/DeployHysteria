using DeployHysteria.UI.Services;
using DeployHysteria.UI.ViewModels;
using FluentAssertions;
using Moq;
using Xunit;

namespace DeployHysteria.UI.Tests;

public class QrCodeDialogViewModelTests
{
    private readonly Mock<IHysteriaClientConfigService> _mockConfigService = new();
    private readonly Mock<IClipboardService> _mockClipboard = new();
    private readonly Mock<IFileSaveService> _mockFileSave = new();
    private readonly LocalizationService _loc = new();

    private readonly ClientConnectionInfo _info = new()
    {
        ServerHost = "vpn.test.com",
        Port = 443,
        Username = "alice",
        Password = "pwd"
    };

    // Valid tiny 1x1 PNG bytes for Bitmap
    private static readonly byte[] TinyPng =
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D,
        0x49, 0x48, 0x44, 0x52, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
        0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4, 0x89, 0x00, 0x00, 0x00,
        0x0A, 0x49, 0x44, 0x41, 0x54, 0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00,
        0x05, 0x00, 0x01, 0x0D, 0x0A, 0x2D, 0xB4, 0x00, 0x00, 0x00, 0x00, 0x49,
        0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82
    ];

    public QrCodeDialogViewModelTests()
    {
        _mockConfigService.Setup(c => c.GenerateUri(It.IsAny<ClientConnectionInfo>()))
            .Returns("hysteria2://alice:pwd@vpn.test.com:443/?sni=vpn.test.com#alice");
        _mockConfigService.Setup(c => c.GenerateTextConfig(It.IsAny<ClientConnectionInfo>()))
            .Returns("=== Config ===");
        _mockConfigService.Setup(c => c.GenerateQrCodePng(It.IsAny<string>(), It.IsAny<int>()))
            .Returns(TinyPng);
    }

    [Fact]
    public async Task CopyUri_ShouldCallClipboard()
    {
        var vm = new QrCodeDialogViewModel(_info, _mockConfigService.Object, _mockClipboard.Object, _mockFileSave.Object, _loc);

        await vm.CopyUriCommand.ExecuteAsync(null);

        _mockClipboard.Verify(c => c.SetTextAsync("hysteria2://alice:pwd@vpn.test.com:443/?sni=vpn.test.com#alice"), Times.Once);
        vm.NotificationMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SaveQrImage_ShouldCallFileSaveService()
    {
        _mockFileSave.Setup(f => f.SaveFileAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("/path/to/qrcode.png");

        var vm = new QrCodeDialogViewModel(_info, _mockConfigService.Object, _mockClipboard.Object, _mockFileSave.Object, _loc);

        await vm.SaveQrImageCommand.ExecuteAsync(null);

        _mockFileSave.Verify(f => f.SaveFileAsync("hysteria2_alice_qrcode.png", It.IsAny<byte[]>(), "PNG Image", "png"), Times.Once);
        vm.NotificationMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ExportTextConfig_ShouldCallFileSaveService()
    {
        _mockFileSave.Setup(f => f.SaveTextFileAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("/path/to/config.txt");

        var vm = new QrCodeDialogViewModel(_info, _mockConfigService.Object, _mockClipboard.Object, _mockFileSave.Object, _loc);

        await vm.ExportTextConfigCommand.ExecuteAsync(null);

        _mockFileSave.Verify(f => f.SaveTextFileAsync("hysteria2_alice_config.txt", "=== Config ==="), Times.Once);
        vm.NotificationMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Close_ShouldSetIsOpenFalse()
    {
        var vm = new QrCodeDialogViewModel(_info, _mockConfigService.Object, _mockClipboard.Object, _mockFileSave.Object, _loc);
        vm.IsOpen.Should().BeTrue();

        vm.CloseCommand.Execute(null);

        vm.IsOpen.Should().BeFalse();
    }
}
