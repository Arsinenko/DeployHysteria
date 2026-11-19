namespace DeployHysteria.Core.Models.Deployment;

public record Fail2BanOptions
{
    public bool Enabled { get; init; } = true;
    public int MaxRetry { get; init; } = 3;
    public TimeSpan BanTime { get; init; } = TimeSpan.FromHours(10);
    public TimeSpan FindTime { get; init; } = TimeSpan.FromMinutes(10);
    public int SshPort { get; init; } = 22;

    public void Validate()
    {
        if (!Enabled) return;

        if (MaxRetry < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxRetry), "Max retry must be at least 1.");
        }

        if (BanTime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(BanTime), "Ban time must be greater than zero.");
        }

        if (FindTime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(FindTime), "Find time must be greater than zero.");
        }

        if (SshPort is < 1 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(SshPort), "SSH port must be between 1 and 65535.");
        }
    }
}
