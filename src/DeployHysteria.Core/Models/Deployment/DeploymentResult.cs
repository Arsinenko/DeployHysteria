namespace DeployHysteria.Core.Models.Deployment;

public record DeploymentResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public string Logs { get; init; } = string.Empty;
    public bool DnsVerified { get; init; }
    public bool PortVerified { get; init; }
    public List<string> ExecutedSteps { get; init; } = [];

    public static DeploymentResult Ok(string message, string logs, bool dnsVerified, bool portVerified, List<string> steps) => new()
    {
        Success = true,
        Message = message,
        Logs = logs,
        DnsVerified = dnsVerified,
        PortVerified = portVerified,
        ExecutedSteps = steps
    };

    public static DeploymentResult Fail(string message, List<string>? steps = null, string logs = "") => new()
    {
        Success = false,
        Message = message,
        Logs = logs,
        ExecutedSteps = steps ?? []
    };
}

public record ServerStatus
{
    public bool ContainerRunning { get; init; }
    public string ContainerStatus { get; init; } = string.Empty;
    public string UfwStatus { get; init; } = string.Empty;
    public string Fail2BanStatus { get; init; } = string.Empty;
    public string RecentLogs { get; init; } = string.Empty;
}
