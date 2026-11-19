using DeployHysteria.Core.Models.Deployment;
using DeployHysteria.Core.Network;
using DeployHysteria.Core.Provisioning;
using DeployHysteria.Core.Security;
using DeployHysteria.Core.Serialization;
using DeployHysteria.Core.Ssh;
using System.Text.RegularExpressions;

namespace DeployHysteria.Core.Deployment;

public partial class HysteriaDeployer : IHysteriaDeployer
{
    private readonly ISshClientFactory _sshClientFactory;
    private readonly IDnsResolver _dnsResolver;
    private readonly IPortChecker _portChecker;
    private readonly IServerProvisioner _provisioner;
    private readonly IHysteriaConfigSerializer _configSerializer;
    private readonly IDockerComposeGenerator _dockerComposeGenerator;

    public HysteriaDeployer(
        ISshClientFactory sshClientFactory,
        IDnsResolver dnsResolver,
        IPortChecker portChecker,
        IServerProvisioner provisioner,
        IHysteriaConfigSerializer configSerializer,
        IDockerComposeGenerator dockerComposeGenerator)
    {
        _sshClientFactory = sshClientFactory ?? throw new ArgumentNullException(nameof(sshClientFactory));
        _dnsResolver = dnsResolver ?? throw new ArgumentNullException(nameof(dnsResolver));
        _portChecker = portChecker ?? throw new ArgumentNullException(nameof(portChecker));
        _provisioner = provisioner ?? throw new ArgumentNullException(nameof(provisioner));
        _configSerializer = configSerializer ?? throw new ArgumentNullException(nameof(configSerializer));
        _dockerComposeGenerator = dockerComposeGenerator ?? throw new ArgumentNullException(nameof(dockerComposeGenerator));
    }

    public async Task<DeploymentResult> DeployAsync(
        DeployOptions options,
        Action<string>? progressCallback = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        var steps = new List<string>();
        bool dnsVerified = false;
        bool portVerified = false;

        try
        {
            // Step 1: DNS verification
            if (options.VerifyDns)
            {
                progressCallback?.Invoke("Verifying DNS records for ACME domains...");
                foreach (var domain in options.HysteriaConfig.Acme.Domains)
                {
                    var domainMatches = await _dnsResolver.VerifyDomainPointsToIpAsync(
                        domain,
                        options.ServerConnection.Host,
                        cancellationToken);

                    if (!domainMatches)
                    {
                        var msg = $"DNS verification failed: domain '{domain}' does not point to target host '{options.ServerConnection.Host}'.";
                        return DeploymentResult.Fail(msg, steps);
                    }
                }

                dnsVerified = true;
                steps.Add("DNS verification passed");
            }

            // Step 2: Connect to server
            progressCallback?.Invoke($"Connecting to {options.ServerConnection.Host}:{options.ServerConnection.Port} via SSH...");
            await using var sshClient = _sshClientFactory.Create(options.ServerConnection);
            await sshClient.ConnectAsync(cancellationToken);
            steps.Add("SSH connection established");

            // Step 3: Check port availability
            var listenPort = ExtractPortFromListen(options.HysteriaConfig.Listen);
            if (options.CheckPort)
            {
                progressCallback?.Invoke($"Checking port {listenPort} availability on server...");
                var portResult = await _portChecker.CheckPortAvailabilityAsync(sshClient, listenPort, cancellationToken);
                if (!portResult.IsAvailable)
                {
                    var msg = $"Port {listenPort} is already in use by: {portResult.OccupiedBy}";
                    return DeploymentResult.Fail(msg, steps);
                }

                portVerified = true;
                steps.Add($"Port {listenPort} availability verified");
            }

            // Step 4: Server provisioning (updates, docker, ufw, fail2ban)
            progressCallback?.Invoke("Starting server provisioning...");
            await _provisioner.ProvisionAsync(sshClient, options, progressCallback, cancellationToken);
            steps.Add("Server provisioning completed");

            // Step 5: Directory preparation
            progressCallback?.Invoke($"Preparing directories in {options.RemoteDirectory}...");
            var escapedDir = ShellSanitizer.Escape(options.RemoteDirectory);
            var escapedAcmeDir = ShellSanitizer.Escape($"{options.RemoteDirectory}/acme");
            await sshClient.ExecuteCommandAsync($"mkdir -p {escapedDir} {escapedAcmeDir}", cancellationToken);

            if (options.HysteriaConfig.Masquerade?.Type?.Equals("file", StringComparison.OrdinalIgnoreCase) == true &&
                options.HysteriaConfig.Masquerade.File != null &&
                !string.IsNullOrWhiteSpace(options.HysteriaConfig.Masquerade.File.Dir))
            {
                var escapedMasq = ShellSanitizer.Escape(options.HysteriaConfig.Masquerade.File.Dir);
                await sshClient.ExecuteCommandAsync($"mkdir -p {escapedMasq}", cancellationToken);
            }
            steps.Add("Directories prepared");

            // Step 6: Generate & upload config.yaml
            progressCallback?.Invoke("Generating and uploading config.yaml...");
            var configYaml = _configSerializer.Serialize(options.HysteriaConfig);
            var remoteConfigPath = $"{options.RemoteDirectory.TrimEnd('/')}/config.yaml";
            await sshClient.UploadTextAsync(remoteConfigPath, configYaml, cancellationToken);
            steps.Add("config.yaml uploaded");

            // Step 7: Generate & upload docker-compose.yml
            progressCallback?.Invoke("Generating and uploading docker-compose.yml...");
            var dockerConfig = _dockerComposeGenerator.CreateDefaultConfig(options.HysteriaConfig);
            var dockerComposeYaml = _dockerComposeGenerator.Generate(dockerConfig);
            var remoteComposePath = $"{options.RemoteDirectory.TrimEnd('/')}/docker-compose.yml";
            await sshClient.UploadTextAsync(remoteComposePath, dockerComposeYaml, cancellationToken);
            steps.Add("docker-compose.yml uploaded");

            // Step 8: Start container
            progressCallback?.Invoke("Starting Hysteria 2 container with Docker Compose...");
            var upCmd = $"cd {escapedDir} && (docker compose up -d || docker-compose up -d)";
            var upResult = await sshClient.ExecuteCommandAsync(upCmd, cancellationToken);
            upResult.EnsureSuccess("Failed to start Hysteria container via docker compose");
            steps.Add("Hysteria container started");

            // Step 9: Fetch container logs
            progressCallback?.Invoke("Retrieving container logs...");
            await Task.Delay(2000, cancellationToken); // brief pause to allow container to initialize
            var logsCmd = $"cd {escapedDir} && (docker compose logs --tail=100 || docker-compose logs --tail=100)";
            var logsResult = await sshClient.ExecuteCommandAsync(logsCmd, cancellationToken);
            steps.Add("Logs retrieved");

            return DeploymentResult.Ok(
                "Hysteria 2 deployed successfully.",
                logsResult.StandardOutput + logsResult.StandardError,
                dnsVerified,
                portVerified,
                steps);
        }
        catch (Exception ex)
        {
            return DeploymentResult.Fail($"Deployment failed: {ex.Message}", steps);
        }
    }

    public static int ExtractPortFromListen(string listen)
    {
        if (string.IsNullOrWhiteSpace(listen)) return 443;

        var match = PortRegex().Match(listen);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var port))
        {
            return port;
        }

        if (int.TryParse(listen, out var directPort))
        {
            return directPort;
        }

        return 443;
    }

    [GeneratedRegex(@"(?::|^)(\d+)$")]
    private static partial Regex PortRegex();
}
