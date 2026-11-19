using DeployHysteria.Core.Models.Hysteria;
using DeployHysteria.Core.Serialization;
using FluentAssertions;
using Xunit;

namespace DeployHysteria.Core.Tests;

public class HysteriaConfigSerializerTests
{
    private readonly HysteriaConfigSerializer _serializer = new();

    [Fact]
    public void Serialize_FullConfig_ShouldMatchExpectedFormat()
    {
        var config = new HysteriaConfig
        {
            Listen = ":443",
            Acme = new AcmeConfig
            {
                Domains = ["vpn.example.com"],
                Email = "admin@example.com",
                Dir = "/acme"
            },
            Acl = new AclConfig
            {
                Inline =
                [
                    "reject(geoip:ru)",
                    "reject(geosite:category-ru)",
                    "direct(all)"
                ]
            },
            Obfs = new ObfsConfig
            {
                Type = "salamander",
                Salamander = new ObfsSalamanderConfig { Password = "super-secret-obfs" }
            },
            Auth = AuthConfig.FromSinglePassword("super-secret-auth"),
            Masquerade = MasqueradeConfig.CreateFile("/var/www/masq"),
            Bandwidth = new BandwidthConfig
            {
                Up = "100 mbps",
                Down = "100 mbps"
            }
        };

        var yaml = _serializer.Serialize(config);

        yaml.Should().Contain("listen: :443");
        yaml.Should().Contain("vpn.example.com");
        yaml.Should().Contain("email: admin@example.com");
        yaml.Should().Contain("dir: /acme");
        yaml.Should().Contain("reject(geoip:ru)");
        yaml.Should().Contain("reject(geosite:category-ru)");
        yaml.Should().Contain("direct(all)");
        yaml.Should().Contain("type: salamander");
        yaml.Should().Contain("password: super-secret-obfs");
        yaml.Should().Contain("type: password");
        yaml.Should().Contain("password: super-secret-auth");
        yaml.Should().Contain("type: file");
        yaml.Should().Contain("dir: /var/www/masq");
        yaml.Should().Contain("up: 100 mbps");
        yaml.Should().Contain("down: 100 mbps");
    }

    [Fact]
    public void Serialize_WithoutObfs_ShouldOmitObfsSection()
    {
        var config = new HysteriaConfig
        {
            Listen = ":443",
            Acme = new AcmeConfig
            {
                Domains = ["vpn.example.com"],
                Email = "admin@example.com"
            },
            Auth = AuthConfig.FromSinglePassword("super-password"),
            Masquerade = MasqueradeConfig.CreateFile()
        };

        var yaml = _serializer.Serialize(config);

        yaml.Should().NotContain("obfs:");
        yaml.Should().NotContain("salamander:");
        yaml.Should().NotContain("bandwidth:");
    }

    [Fact]
    public void Serialize_WithUserpass_ShouldIncludeUsers()
    {
        var config = new HysteriaConfig
        {
            Listen = ":443",
            Acme = new AcmeConfig
            {
                Domains = ["vpn.example.com"],
                Email = "admin@example.com"
            },
            Auth = new AuthConfig
            {
                Type = "userpass",
                Userpass = new Dictionary<string, string>
                {
                    ["alice"] = "pass1",
                    ["bob"] = "pass2"
                }
            }
        };

        var yaml = _serializer.Serialize(config);

        yaml.Should().Contain("type: userpass");
        yaml.Should().Contain("alice: pass1");
        yaml.Should().Contain("bob: pass2");
    }

    [Fact]
    public void Deserialize_ValidYaml_ShouldProduceCorrectConfig()
    {
        const string yaml = """
            listen: :443
            acme:
              domains:
                - vpn.example.com
              email: admin@example.com
              dir: /acme
            acl:
              inline:
                - reject(geoip:ru)
                - direct(all)
            obfs:
              type: salamander
              salamander:
                password: secret-obfs
            auth:
              type: password
              password: secret-password
            masquerade:
              type: file
              file:
                dir: /var/www/masq
            bandwidth:
              up: 50 mbps
              down: 50 mbps
            """;

        var config = _serializer.Deserialize(yaml);

        config.Listen.Should().Be(":443");
        config.Acme.Domains.Should().ContainSingle().Which.Should().Be("vpn.example.com");
        config.Acme.Email.Should().Be("admin@example.com");
        config.Acl.Should().NotBeNull();
        config.Acl!.Inline.Should().HaveCount(2);
        config.Obfs.Should().NotBeNull();
        config.Obfs!.Salamander!.Password.Should().Be("secret-obfs");
        config.Auth.Type.Should().Be("password");
        config.Auth.Password.Should().Be("secret-password");
        config.Masquerade.Should().NotBeNull();
        config.Masquerade!.File!.Dir.Should().Be("/var/www/masq");
        config.Bandwidth.Should().NotBeNull();
        config.Bandwidth!.Up.Should().Be("50 mbps");
    }

    [Fact]
    public void Roundtrip_SerializeAndDeserialize_ShouldBeEqual()
    {
        var original = new HysteriaConfig
        {
            Listen = ":443",
            Acme = new AcmeConfig
            {
                Domains = ["node1.vpn.com", "node2.vpn.com"],
                Email = "ops@vpn.com",
                Dir = "/acme"
            },
            Auth = new AuthConfig
            {
                Type = "userpass",
                Userpass = new Dictionary<string, string>
                {
                    ["user1"] = "pass1",
                    ["user2"] = "pass2"
                }
            },
            Masquerade = MasqueradeConfig.CreateProxy("https://bing.com"),
            Bandwidth = new BandwidthConfig { Up = "10 mbps", Down = "20 mbps" }
        };

        var yaml = _serializer.Serialize(original);
        var restored = _serializer.Deserialize(yaml);

        restored.Listen.Should().Be(original.Listen);
        restored.Acme.Domains.Should().BeEquivalentTo(original.Acme.Domains);
        restored.Acme.Email.Should().Be(original.Acme.Email);
        restored.Auth.Type.Should().Be("userpass");
        restored.Auth.Userpass.Should().BeEquivalentTo(original.Auth.Userpass);
        restored.Masquerade!.Type.Should().Be("proxy");
        restored.Masquerade!.Proxy!.Url.Should().Be("https://bing.com");
        restored.Bandwidth!.Up.Should().Be("10 mbps");
        restored.Bandwidth!.Down.Should().Be("20 mbps");
    }
}
