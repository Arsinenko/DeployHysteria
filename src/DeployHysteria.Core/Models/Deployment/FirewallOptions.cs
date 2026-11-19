namespace DeployHysteria.Core.Models.Deployment;

public record FirewallOptions
{
    public bool Enabled { get; init; } = true;
    public int SshPort { get; init; } = 22;
    public int HysteriaPort { get; init; } = 443;

    public void Validate()
    {
        if (!Enabled) return;

        if (SshPort is < 1 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(SshPort), "SSH port must be between 1 and 65535.");
        }

        if (HysteriaPort is < 1 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(HysteriaPort), "Hysteria port must be between 1 and 65535.");
        }
    }
}
