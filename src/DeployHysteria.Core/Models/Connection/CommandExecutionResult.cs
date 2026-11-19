namespace DeployHysteria.Core.Models.Connection;

public record CommandExecutionResult(
    int ExitCode,
    string StandardOutput,
    string StandardError)
{
    public bool Success => ExitCode == 0;

    public void EnsureSuccess(string? message = null)
    {
        if (!Success)
        {
            var details = string.IsNullOrWhiteSpace(StandardError) ? StandardOutput : StandardError;
            var errorMsg = message != null
                ? $"{message} (Exit code {ExitCode}): {details}"
                : $"Command failed with exit code {ExitCode}: {details}";
            throw new InvalidOperationException(errorMsg);
        }
    }
}
