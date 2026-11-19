using DeployHysteria.Core.Models.Connection;

namespace DeployHysteria.Core.Ssh;

public interface ISshClient : IDisposable, IAsyncDisposable
{
    bool IsConnected { get; }
    Task ConnectAsync(CancellationToken cancellationToken = default);
    void Disconnect();
    Task<CommandExecutionResult> ExecuteCommandAsync(string command, CancellationToken cancellationToken = default);
    Task UploadTextAsync(string remoteFilePath, string content, CancellationToken cancellationToken = default);
    Task<string> DownloadTextAsync(string remoteFilePath, CancellationToken cancellationToken = default);
    Task<bool> FileExistsAsync(string remoteFilePath, CancellationToken cancellationToken = default);
    Task CreateDirectoryAsync(string remoteDirectoryPath, CancellationToken cancellationToken = default);
}

public interface ISshClientFactory
{
    ISshClient Create(ServerConnectionInfo connectionInfo);
}
