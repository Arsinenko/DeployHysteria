using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace DeployHysteria.UI.Services;

public class AvaloniaClipboardService : IClipboardService
{
    public async Task SetTextAsync(string text)
    {
        var topLevel = GetTopLevel();
        if (topLevel?.Clipboard != null)
        {
            var transfer = new Avalonia.Input.DataTransfer();
            transfer.Add(Avalonia.Input.DataTransferItem.CreateText(text));
            await topLevel.Clipboard.SetDataAsync(transfer);
        }
    }

    private static TopLevel? GetTopLevel()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }

        if (Application.Current?.ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            return TopLevel.GetTopLevel(singleView.MainView);
        }

        return null;
    }
}

public class AvaloniaFileSaveService : IFileSaveService
{
    public async Task<string?> SaveFileAsync(string defaultFileName, byte[] data, string fileTypeDescription, string extension)
    {
        var topLevel = GetTopLevel();
        if (topLevel?.StorageProvider != null && topLevel.StorageProvider.CanSave)
        {
            var options = new FilePickerSaveOptions
            {
                Title = "Save File",
                SuggestedFileName = defaultFileName,
                DefaultExtension = extension,
                FileTypeChoices =
                [
                    new FilePickerFileType(fileTypeDescription)
                    {
                        Patterns = [$"*.{extension.TrimStart('.')}"]
                    }
                ]
            };

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(options);
            if (file != null)
            {
                await using var stream = await file.OpenWriteAsync();
                await stream.WriteAsync(data);
                return file.Path.LocalPath;
            }

            return null;
        }

        // Fallback: save to Downloads or current directory
        var fallbackPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", defaultFileName);
        var dir = Path.GetDirectoryName(fallbackPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        await File.WriteAllBytesAsync(fallbackPath, data);
        return fallbackPath;
    }

    public async Task<string?> SaveTextFileAsync(string defaultFileName, string content)
    {
        return await SaveFileAsync(defaultFileName, System.Text.Encoding.UTF8.GetBytes(content), "Text File", "txt");
    }

    private static TopLevel? GetTopLevel()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }

        if (Application.Current?.ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            return TopLevel.GetTopLevel(singleView.MainView);
        }

        return null;
    }
}
