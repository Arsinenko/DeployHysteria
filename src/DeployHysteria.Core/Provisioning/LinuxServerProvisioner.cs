using DeployHysteria.Core.Models.Deployment;
using DeployHysteria.Core.Security;
using DeployHysteria.Core.Ssh;
using System.Text;

namespace DeployHysteria.Core.Provisioning;

public class LinuxServerProvisioner : IServerProvisioner
{
    public async Task ProvisionAsync(
        ISshClient sshClient,
        DeployOptions options,
        Action<string>? progressCallback = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sshClient);
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        if (options.UpdatePackages)
        {
            progressCallback?.Invoke("Updating system packages...");
            await UpdateSystemPackagesAsync(sshClient, cancellationToken);
        }

        if (options.InstallDocker)
        {
            progressCallback?.Invoke("Checking and installing Docker and Docker Compose...");
            await InstallDockerAsync(sshClient, cancellationToken);
        }

        if (options.Firewall.Enabled)
        {
            progressCallback?.Invoke($"Configuring UFW firewall (SSH: {options.Firewall.SshPort}, Hysteria: {options.Firewall.HysteriaPort})...");
            await ConfigureFirewallAsync(sshClient, options.Firewall, cancellationToken);
        }

        if (options.Fail2Ban.Enabled)
        {
            progressCallback?.Invoke($"Configuring Fail2Ban (MaxRetry: {options.Fail2Ban.MaxRetry}, BanTime: {options.Fail2Ban.BanTime})...");
            await ConfigureFail2BanAsync(sshClient, options.Fail2Ban, cancellationToken);
        }
    }

    public virtual async Task UpdateSystemPackagesAsync(ISshClient sshClient, CancellationToken cancellationToken)
    {
        // Safe Debian/Ubuntu package update
        const string cmd = "DEBIAN_FRONTEND=noninteractive apt-get update -y && " +
                           "DEBIAN_FRONTEND=noninteractive apt-get upgrade -y";
        var res = await sshClient.ExecuteCommandAsync(cmd, cancellationToken);
        res.EnsureSuccess("System package update failed");
    }

    public virtual async Task InstallDockerAsync(ISshClient sshClient, CancellationToken cancellationToken)
    {
        // Check if docker is installed
        var checkDocker = await sshClient.ExecuteCommandAsync("command -v docker || true", cancellationToken);
        if (string.IsNullOrWhiteSpace(checkDocker.StandardOutput))
        {
            // Install curl and ca-certificates first
            await sshClient.ExecuteCommandAsync("DEBIAN_FRONTEND=noninteractive apt-get install -y curl ca-certificates", cancellationToken);

            // Install docker using official installation script
            const string installDockerCmd = "curl -fsSL https://get.docker.com -o /tmp/get-docker.sh && " +
                                            "sh /tmp/get-docker.sh && " +
                                            "rm -f /tmp/get-docker.sh";
            var installRes = await sshClient.ExecuteCommandAsync(installDockerCmd, cancellationToken);
            installRes.EnsureSuccess("Docker installation failed");
        }

        // Check if docker compose (plugin or standalone) is installed
        var checkCompose = await sshClient.ExecuteCommandAsync("docker compose version || docker-compose version || true", cancellationToken);
        if (string.IsNullOrWhiteSpace(checkCompose.StandardOutput))
        {
            await sshClient.ExecuteCommandAsync("DEBIAN_FRONTEND=noninteractive apt-get install -y docker-compose-plugin || DEBIAN_FRONTEND=noninteractive apt-get install -y docker-compose", cancellationToken);
        }

        // Enable and start Docker service
        await sshClient.ExecuteCommandAsync("systemctl enable --now docker", cancellationToken);
    }

    public virtual async Task ConfigureFirewallAsync(ISshClient sshClient, FirewallOptions firewall, CancellationToken cancellationToken)
    {
        // Ensure ufw is installed
        await sshClient.ExecuteCommandAsync("DEBIAN_FRONTEND=noninteractive apt-get install -y ufw", cancellationToken);

        // Open SSH port
        var sshRule = $"ufw allow {firewall.SshPort}/tcp";
        var sshRes = await sshClient.ExecuteCommandAsync(sshRule, cancellationToken);
        sshRes.EnsureSuccess($"Failed to allow SSH port {firewall.SshPort} in UFW");

        // Open Hysteria port (both TCP and UDP)
        var hysteriaTcp = $"ufw allow {firewall.HysteriaPort}/tcp";
        var hysteriaUdp = $"ufw allow {firewall.HysteriaPort}/udp";

        var tcpRes = await sshClient.ExecuteCommandAsync(hysteriaTcp, cancellationToken);
        tcpRes.EnsureSuccess($"Failed to allow Hysteria TCP port {firewall.HysteriaPort} in UFW");

        var udpRes = await sshClient.ExecuteCommandAsync(hysteriaUdp, cancellationToken);
        udpRes.EnsureSuccess($"Failed to allow Hysteria UDP port {firewall.HysteriaPort} in UFW");

        // Enable UFW non-interactively
        var enableRes = await sshClient.ExecuteCommandAsync("ufw --force enable", cancellationToken);
        enableRes.EnsureSuccess("Failed to enable UFW");
    }

    public virtual async Task ConfigureFail2BanAsync(ISshClient sshClient, Fail2BanOptions fail2ban, CancellationToken cancellationToken)
    {
        // Ensure fail2ban is installed
        await sshClient.ExecuteCommandAsync("DEBIAN_FRONTEND=noninteractive apt-get install -y fail2ban", cancellationToken);

        var banTimeSeconds = (int)fail2ban.BanTime.TotalSeconds;
        var findTimeSeconds = (int)fail2ban.FindTime.TotalSeconds;

        var configBuilder = new StringBuilder();
        configBuilder.AppendLine("# Generated by DeployHysteria");
        configBuilder.AppendLine("[DEFAULT]");
        configBuilder.AppendLine($"bantime = {banTimeSeconds}");
        configBuilder.AppendLine($"findtime = {findTimeSeconds}");
        configBuilder.AppendLine($"maxretry = {fail2ban.MaxRetry}");
        configBuilder.AppendLine();
        configBuilder.AppendLine("[sshd]");
        configBuilder.AppendLine("enabled = true");
        configBuilder.AppendLine($"port = {fail2ban.SshPort}");
        configBuilder.AppendLine($"maxretry = {fail2ban.MaxRetry}");
        configBuilder.AppendLine($"bantime = {banTimeSeconds}");
        configBuilder.AppendLine($"findtime = {findTimeSeconds}");

        const string confPath = "/etc/fail2ban/jail.d/hysteria-ssh.local";
        await sshClient.UploadTextAsync(confPath, configBuilder.ToString(), cancellationToken);

        // Enable and restart fail2ban
        var restartRes = await sshClient.ExecuteCommandAsync("systemctl enable --now fail2ban && systemctl restart fail2ban", cancellationToken);
        restartRes.EnsureSuccess("Failed to start/restart fail2ban");
    }
}
