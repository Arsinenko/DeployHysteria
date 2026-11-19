namespace DeployHysteria.UI.Services;

public interface IClipboardService
{
    Task SetTextAsync(string text);
}

public interface IFileSaveService
{
    Task<string?> SaveFileAsync(string defaultFileName, byte[] data, string fileTypeDescription, string extension);
    Task<string?> SaveTextFileAsync(string defaultFileName, string content);
}
