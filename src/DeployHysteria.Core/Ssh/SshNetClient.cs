using DeployHysteria.Core.Models.Connection;
using DeployHysteria.Core.Security;
using Renci.SshNet;
using System.Text;

namespace DeployHysteria.Core.Ssh;

public class SshNetClient : ISshClient
{
    private readonly Renci.SshNet.SshClient _sshClient;
    private readonly Renci.SshNet.SftpClient _sftpClient;
    private bool _disposed;

    public SshNetClient(ServerConnectionInfo connectionInfo)
    {
        ArgumentNullException.ThrowIfNull(connectionInfo);
        connectionInfo.Validate();

        var authMethods = new List<AuthenticationMethod>();

        if (!string.IsNullOrWhiteSpace(connectionInfo.Password))
        {
            authMethods.Add(new PasswordAuthenticationMethod(connectionInfo.Username, connectionInfo.Password));
        }

        if (!string.IsNullOrWhiteSpace(connectionInfo.SshPrivateKey))
        {
            using var keyStream = new MemoryStream(Encoding.UTF8.GetBytes(connectionInfo.SshPrivateKey));
            var keyFile = string.IsNullOrWhiteSpace(connectionInfo.SshPrivateKeyPassphrase)
                ? new PrivateKeyFile(keyStream)
                : new PrivateKeyFile(keyStream, connectionInfo.SshPrivateKeyPassphrase);

            authMethods.Add(new PrivateKeyAuthenticationMethod(connectionInfo.Username, keyFile));
        }

        var sshConnectionInfo = new Renci.SshNet.ConnectionInfo(
            connectionInfo.Host,
            connectionInfo.Port,
            connectionInfo.Username,
            authMethods.ToArray());

        _sshClient = new Renci.SshNet.SshClient(sshConnectionInfo);
        _sftpClient = new Renci.SshNet.SftpClient(sshConnectionInfo);
    }

    public bool IsConnected => _sshClient.IsConnected && _sftpClient.IsConnected;

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Task.Run(() =>
        {
            if (!_sshClient.IsConnected)
            {
                _sshClient.Connect();
            }

            if (!_sftpClient.IsConnected)
            {
                _sftpClient.Connect();
            }
        }, cancellationToken);
    }

    public void Disconnect()
    {
        if (_sftpClient.IsConnected) _sftpClient.Disconnect();
        if (_sshClient.IsConnected) _sshClient.Disconnect();
    }

    public async Task<CommandExecutionResult> ExecuteCommandAsync(string command, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await Task.Run(() =>
        {
            using var cmd = _sshClient.CreateCommand(command);
            cmd.CommandTimeout = TimeSpan.FromMinutes(10);
            var output = cmd.Execute();

            return new CommandExecutionResult(
                cmd.ExitStatus ?? -1,
                output ?? string.Empty,
                cmd.Error ?? string.Empty);
        }, cancellationToken);
    }

    public async Task UploadTextAsync(string remoteFilePath, string content, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Task.Run(() =>
        {
            var directory = Path.GetDirectoryName(remoteFilePath)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(directory))
            {
                CreateDirectoryRecursive(directory);
            }

            using var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            _sftpClient.UploadFile(memoryStream, remoteFilePath, canOverride: true);
        }, cancellationToken);
    }

    public async Task<string> DownloadTextAsync(string remoteFilePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await Task.Run(() =>
        {
            using var memoryStream = new MemoryStream();
            _sftpClient.DownloadFile(remoteFilePath, memoryStream);
            return Encoding.UTF8.GetString(memoryStream.ToArray());
        }, cancellationToken);
    }

    public async Task<bool> FileExistsAsync(string remoteFilePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await Task.Run(() => _sftpClient.Exists(remoteFilePath), cancellationToken);
    }

    public async Task CreateDirectoryAsync(string remoteDirectoryPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Task.Run(() => CreateDirectoryRecursive(remoteDirectoryPath), cancellationToken);
    }

    private void CreateDirectoryRecursive(string path)
    {
        var normalized = path.Replace('\\', '/');
        var parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var current = normalized.StartsWith('/') ? "/" : "";

        foreach (var part in parts)
        {
            current = current == "/" ? $"/{part}" : $"{current}/{part}";
            if (!_sftpClient.Exists(current))
            {
                try
                {
                    _sftpClient.CreateDirectory(current);
                }
                catch (Renci.SshNet.Common.SshException)
                {
                    // Directory might have been created concurrently or already exists
                    if (!_sftpClient.Exists(current))
                    {
                        throw;
                    }
                }
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try { _sftpClient.Dispose(); } catch { }
        try { _sshClient.Dispose(); } catch { }
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        Dispose();
        await Task.CompletedTask;
    }
}

public class SshNetClientFactory : ISshClientFactory
{
    public ISshClient Create(ServerConnectionInfo connectionInfo)
    {
        return new SshNetClient(connectionInfo);
    }
}
