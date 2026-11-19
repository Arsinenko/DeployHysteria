using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeployHysteria.UI.Services;

namespace DeployHysteria.UI.ViewModels;

public partial class QrCodeDialogViewModel : ViewModelBase
{
    private readonly IClipboardService _clipboardService;
    private readonly IFileSaveService _fileSaveService;
    private readonly ILocalizationService _loc;
    private readonly byte[] _qrPngBytes;

    [ObservableProperty]
    private Bitmap? _qrBitmap;

    [ObservableProperty]
    private string _uri = string.Empty;

    [ObservableProperty]
    private string _textConfig = string.Empty;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _notificationMessage = string.Empty;

    [ObservableProperty]
    private bool _isOpen;

    public ILocalizationService Loc => _loc;

    public QrCodeDialogViewModel(
        ClientConnectionInfo info,
        IHysteriaClientConfigService clientConfigService,
        IClipboardService clipboardService,
        IFileSaveService fileSaveService,
        ILocalizationService loc)
    {
        _clipboardService = clipboardService;
        _fileSaveService = fileSaveService;
        _loc = loc;

        Username = !string.IsNullOrWhiteSpace(info.Username) ? info.Username : "default";
        Uri = clientConfigService.GenerateUri(info);
        TextConfig = clientConfigService.GenerateTextConfig(info);

        _qrPngBytes = clientConfigService.GenerateQrCodePng(Uri, pixelsPerModule: 10);
        try
        {
            using var stream = new MemoryStream(_qrPngBytes);
            QrBitmap = new Bitmap(stream);
        }
        catch (InvalidOperationException)
        {
            // Allowed in headless/mock unit tests where Avalonia rendering platform is not initialized
        }
        IsOpen = true;
    }

    [RelayCommand]
    private async Task CopyUriAsync()
    {
        try
        {
            await _clipboardService.SetTextAsync(Uri);
            ShowNotification(_loc["CopiedToClipboard"]);
        }
        catch (Exception ex)
        {
            ShowNotification(ex.Message);
        }
    }

    [RelayCommand]
    private async Task SaveQrImageAsync()
    {
        try
        {
            var fileName = $"hysteria2_{Username}_qrcode.png";
            var path = await _fileSaveService.SaveFileAsync(fileName, _qrPngBytes, "PNG Image", "png");
            if (!string.IsNullOrEmpty(path))
            {
                ShowNotification(_loc["SavedToFile"]);
            }
        }
        catch (Exception ex)
        {
            ShowNotification(ex.Message);
        }
    }

    [RelayCommand]
    private async Task ExportTextConfigAsync()
    {
        try
        {
            var fileName = $"hysteria2_{Username}_config.txt";
            var path = await _fileSaveService.SaveTextFileAsync(fileName, TextConfig);
            if (!string.IsNullOrEmpty(path))
            {
                ShowNotification(_loc["SavedToFile"]);
            }
        }
        catch (Exception ex)
        {
            ShowNotification(ex.Message);
        }
    }

    [RelayCommand]
    private void Close()
    {
        IsOpen = false;
    }

    private void ShowNotification(string msg)
    {
        NotificationMessage = msg;
        Task.Delay(3000).ContinueWith(_ =>
        {
            if (NotificationMessage == msg)
            {
                NotificationMessage = string.Empty;
            }
        });
    }
}
