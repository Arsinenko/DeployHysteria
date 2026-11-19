using DeployHysteria.Core.Models.Docker;
using DeployHysteria.Core.Models.Hysteria;

namespace DeployHysteria.Core.Serialization;

public interface IDockerComposeGenerator
{
    string Generate(DockerComposeConfig config);
    DockerComposeConfig CreateDefaultConfig(HysteriaConfig? hysteriaConfig = null);
}
