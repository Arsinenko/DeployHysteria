using Avalonia.Platform.Storage;

namespace DeployHysteria.UI.Services;

public class AvaloniaClipboardService : IClipboardService
{
    public async Task SetTextAsync(string text)
    {
        var topLevel = TopLevelProvider.GetTopLevel();
        if (topLevel?.Clipboard != null)
        {
            var transfer = new Avalonia.Input.DataTransfer();
            transfer.Add(Avalonia.Input.DataTransferItem.CreateText(text));
            await topLevel.Clipboard.SetDataAsync(transfer);
        }
    }
}

public class AvaloniaFileSaveService : IFileSaveService
{
    public async Task<string?> SaveFileAsync(string defaultFileName, byte[] data, string fileTypeDescription, string extension)
    {
        var topLevel = TopLevelProvider.GetTopLevel();
        if (topLevel?.StorageProvider != null && topLevel.StorageProvider.CanSave)
        {
            var cleanExt = extension.TrimStart('.');
            var options = new FilePickerSaveOptions
            {
                Title = "Save File",
                SuggestedFileName = defaultFileName,
                DefaultExtension = cleanExt,
                FileTypeChoices =
                [
                    new FilePickerFileType(fileTypeDescription)
                    {
                        Patterns = [$"*.{cleanExt}"],
                        MimeTypes = GetMimeTypes(cleanExt)
                    }
                ]
            };

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(options);
            if (file != null)
            {
                await using var stream = await file.OpenWriteAsync();
                await stream.WriteAsync(data);
                await stream.FlushAsync();
                return file.Path?.IsFile == true
                    ? file.Path.LocalPath
                    : file.Path?.ToString() ?? file.Name;
            }

            return null;
        }

        // Fallback: save to Downloads or app documents directory
        var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrWhiteSpace(baseDir))
        {
            baseDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }
        if (string.IsNullOrWhiteSpace(baseDir))
        {
            baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }

        var fallbackPath = Path.Combine(baseDir, "Downloads", defaultFileName);
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

    private static IReadOnlyList<string> GetMimeTypes(string extension) => extension.ToLowerInvariant() switch
    {
        "png" => ["image/png"],
        "jpg" or "jpeg" => ["image/jpeg"],
        "txt" => ["text/plain"],
        "json" => ["application/json"],
        "yaml" or "yml" => ["text/yaml", "application/x-yaml"],
        _ => ["application/octet-stream"]
    };
}
