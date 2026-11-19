using DeployHysteria.Core.Models.Hysteria;

namespace DeployHysteria.UI.Services;

public record ClientConnectionInfo
{
    public required string ServerHost { get; init; }
    public int Port { get; init; } = 443;
    public string? Username { get; init; }
    public required string Password { get; init; }
    public string? Sni { get; init; }
    public string? ObfsType { get; init; }
    public string? ObfsPassword { get; init; }
    public bool Insecure { get; init; } = false;
    public string Tag { get; init; } = "Hysteria2";
}

public interface IHysteriaClientConfigService
{
    string GenerateUri(ClientConnectionInfo info);
    string GenerateTextConfig(ClientConnectionInfo info);
    byte[] GenerateQrCodePng(string content, int pixelsPerModule = 15);
    ClientConnectionInfo CreateFromConfig(HysteriaConfig config, string username, string password, string? host = null);
}
