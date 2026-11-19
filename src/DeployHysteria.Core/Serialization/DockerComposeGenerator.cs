using DeployHysteria.Core.Models.Docker;
using DeployHysteria.Core.Models.Hysteria;
using System.Text;

namespace DeployHysteria.Core.Serialization;

public class DockerComposeGenerator : IDockerComposeGenerator
{
    public DockerComposeConfig CreateDefaultConfig(HysteriaConfig? hysteriaConfig = null)
    {
        var config = new DockerComposeConfig();

        if (hysteriaConfig?.Masquerade?.Type?.Equals("file", StringComparison.OrdinalIgnoreCase) == true &&
            hysteriaConfig.Masquerade.File != null &&
            !string.IsNullOrWhiteSpace(hysteriaConfig.Masquerade.File.Dir))
        {
            var dir = hysteriaConfig.Masquerade.File.Dir;
            config.AdditionalVolumes.Add($"{dir}:{dir}:ro");
        }

        return config;
    }

    public string Generate(DockerComposeConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var sb = new StringBuilder();
        sb.AppendLine("services:");
        sb.AppendLine("  hysteria:");
        sb.AppendLine($"    image: {config.Image}");
        sb.AppendLine($"    container_name: {config.ContainerName}");
        sb.AppendLine($"    restart: {config.Restart}");
        sb.AppendLine();
        sb.AppendLine($"    network_mode: {config.NetworkMode}");
        sb.AppendLine();
        sb.AppendLine("    volumes:");
        sb.AppendLine($"      - {config.ConfigHostPath}:/etc/hysteria/config.yaml:ro");
        sb.AppendLine($"      - {config.AcmeHostPath}:/acme");

        foreach (var vol in config.AdditionalVolumes)
        {
            sb.AppendLine($"      - {vol}");
        }

        sb.AppendLine();
        sb.AppendLine("    command: [\"server\", \"-c\", \"/etc/hysteria/config.yaml\"]");
        sb.AppendLine();
        sb.AppendLine("    cap_drop:");
        foreach (var cap in config.CapDrop)
        {
            sb.AppendLine($"      - {cap}");
        }

        sb.AppendLine("    cap_add:");
        foreach (var cap in config.CapAdd)
        {
            sb.AppendLine($"      - {cap}");
        }

        sb.AppendLine();
        sb.AppendLine("    security_opt:");
        foreach (var sec in config.SecurityOpt)
        {
            sb.AppendLine($"      - {sec}");
        }

        return sb.ToString();
    }
}
