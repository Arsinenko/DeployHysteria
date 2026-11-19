namespace DeployHysteria.Core.Models.Connection;

public record ServerConnectionInfo
{
    public required string Host { get; init; }
    public int Port { get; init; } = 22;
    public required string Username { get; init; }
    public string? Password { get; init; }
    public string? SshPrivateKey { get; init; }
    public string? SshPrivateKeyPassphrase { get; init; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Host))
        {
            throw new ArgumentException("Server host cannot be empty.", nameof(Host));
        }

        if (Port is < 1 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(Port), Port, "Port must be between 1 and 65535.");
        }

        if (string.IsNullOrWhiteSpace(Username))
        {
            throw new ArgumentException("Username cannot be empty.", nameof(Username));
        }

        if (string.IsNullOrWhiteSpace(Password) && string.IsNullOrWhiteSpace(SshPrivateKey))
        {
            throw new ArgumentException("Either password or SSH private key must be provided.");
        }
    }
}
