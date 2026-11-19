using DeployHysteria.Core.Models.Connection;
using DeployHysteria.Core.Models.Hysteria;

namespace DeployHysteria.Core.Models.Deployment;

public record DeployOptions
{
    public required ServerConnectionInfo ServerConnection { get; init; }
    public required HysteriaConfig HysteriaConfig { get; init; }
    public string RemoteDirectory { get; init; } = "/opt/hysteria2";
    public bool UpdatePackages { get; init; } = true;
    public bool InstallDocker { get; init; } = true;
    public FirewallOptions Firewall { get; init; } = new();
    public Fail2BanOptions Fail2Ban { get; init; } = new();
    public bool VerifyDns { get; init; } = true;
    public bool CheckPort { get; init; } = true;

    public void Validate()
    {
        ServerConnection.Validate();
        HysteriaConfig.Validate();
        Firewall.Validate();
        Fail2Ban.Validate();

        if (string.IsNullOrWhiteSpace(RemoteDirectory))
        {
            throw new ArgumentException("Remote directory cannot be empty.", nameof(RemoteDirectory));
        }
    }
}
