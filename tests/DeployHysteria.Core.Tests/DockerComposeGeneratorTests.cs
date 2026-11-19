using DeployHysteria.Core.Models.Docker;
using DeployHysteria.Core.Models.Hysteria;
using DeployHysteria.Core.Serialization;
using FluentAssertions;
using Xunit;

namespace DeployHysteria.Core.Tests;

public class DockerComposeGeneratorTests
{
    private readonly DockerComposeGenerator _generator = new();

    [Fact]
    public void Generate_DefaultConfig_ShouldProduceValidDockerCompose()
    {
        var config = new DockerComposeConfig();
        var yaml = _generator.Generate(config);

        yaml.Should().Contain("services:");
        yaml.Should().Contain("hysteria:");
        yaml.Should().Contain("image: tobyxdd/hysteria:latest");
        yaml.Should().Contain("container_name: hysteria2");
        yaml.Should().Contain("restart: unless-stopped");
        yaml.Should().Contain("network_mode: host");
        yaml.Should().Contain("./config.yaml:/etc/hysteria/config.yaml:ro");
        yaml.Should().Contain("./acme:/acme");
        yaml.Should().Contain("command: [\"server\", \"-c\", \"/etc/hysteria/config.yaml\"]");
        yaml.Should().Contain("- ALL");
        yaml.Should().Contain("- NET_BIND_SERVICE");
        yaml.Should().Contain("- no-new-privileges:true");
    }

    [Fact]
    public void CreateDefaultConfig_WithFileMasquerade_ShouldIncludeMasqueradeVolume()
    {
        var hysteriaConfig = new HysteriaConfig
        {
            Masquerade = MasqueradeConfig.CreateFile("/var/www/masq")
        };

        var config = _generator.CreateDefaultConfig(hysteriaConfig);
        var yaml = _generator.Generate(config);

        yaml.Should().Contain("/var/www/masq:/var/www/masq:ro");
    }
}
