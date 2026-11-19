namespace DeployHysteria.Core.Models.Docker;

public record DockerComposeConfig
{
    public string Image { get; init; } = "tobyxdd/hysteria:latest";
    public string ContainerName { get; init; } = "hysteria2";
    public string Restart { get; init; } = "unless-stopped";
    public string NetworkMode { get; init; } = "host";
    public string ConfigHostPath { get; init; } = "./config.yaml";
    public string AcmeHostPath { get; init; } = "./acme";
    public List<string> AdditionalVolumes { get; init; } = [];
    public List<string> Command { get; init; } = ["server", "-c", "/etc/hysteria/config.yaml"];
    public List<string> CapDrop { get; init; } = ["ALL"];
    public List<string> CapAdd { get; init; } = ["NET_BIND_SERVICE"];
    public List<string> SecurityOpt { get; init; } = ["no-new-privileges:true"];
}
