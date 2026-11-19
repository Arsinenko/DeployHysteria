using DeployHysteria.Core.Models.Hysteria;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DeployHysteria.Core.Serialization;

public class HysteriaConfigSerializer : IHysteriaConfigSerializer
{
    private readonly ISerializer _serializer;
    private readonly IDeserializer _deserializer;

    public HysteriaConfigSerializer()
    {
        _serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitEmptyCollections)
            .Build();

        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    public string Serialize(HysteriaConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        config.Validate();

        // Create a normalized clone so we don't accidentally mutate the input config
        var clone = CloneAndClean(config);
        return _serializer.Serialize(clone);
    }

    public HysteriaConfig Deserialize(string yaml)
    {
        if (string.IsNullOrWhiteSpace(yaml))
        {
            throw new ArgumentException("YAML content cannot be null or empty.", nameof(yaml));
        }

        var config = _deserializer.Deserialize<HysteriaConfig>(yaml);
        if (config == null)
        {
            throw new InvalidOperationException("Failed to deserialize Hysteria configuration from YAML.");
        }

        config.Validate();
        return config;
    }

    private static HysteriaConfig CloneAndClean(HysteriaConfig source)
    {
        var target = new HysteriaConfig
        {
            Listen = source.Listen,
            Acme = new AcmeConfig
            {
                Domains = [..source.Acme.Domains],
                Email = source.Acme.Email,
                Dir = source.Acme.Dir
            }
        };

        if (source.Acl?.Inline != null && source.Acl.Inline.Count > 0)
        {
            target.Acl = new AclConfig { Inline = [..source.Acl.Inline] };
        }

        if (source.Obfs != null && !string.IsNullOrWhiteSpace(source.Obfs.Salamander?.Password))
        {
            target.Obfs = new ObfsConfig
            {
                Type = string.IsNullOrWhiteSpace(source.Obfs.Type) ? "salamander" : source.Obfs.Type,
                Salamander = new ObfsSalamanderConfig
                {
                    Password = source.Obfs.Salamander.Password
                }
            };
        }

        if (source.Auth.Type.Equals("userpass", StringComparison.OrdinalIgnoreCase))
        {
            target.Auth = new AuthConfig
            {
                Type = "userpass",
                Userpass = source.Auth.Userpass != null ? new Dictionary<string, string>(source.Auth.Userpass) : []
            };
        }
        else
        {
            target.Auth = new AuthConfig
            {
                Type = "password",
                Password = source.Auth.Password
            };
        }

        if (source.Masquerade != null)
        {
            target.Masquerade = new MasqueradeConfig
            {
                Type = source.Masquerade.Type
            };

            if (source.Masquerade.File != null)
            {
                target.Masquerade.File = new MasqueradeFileConfig { Dir = source.Masquerade.File.Dir };
            }

            if (source.Masquerade.Proxy != null)
            {
                target.Masquerade.Proxy = new MasqueradeProxyConfig
                {
                    Url = source.Masquerade.Proxy.Url,
                    RewriteHost = source.Masquerade.Proxy.RewriteHost
                };
            }

            if (!string.IsNullOrWhiteSpace(source.Masquerade.String))
            {
                target.Masquerade.String = source.Masquerade.String;
            }
        }

        if (source.Bandwidth != null && source.Bandwidth.HasLimit)
        {
            target.Bandwidth = new BandwidthConfig
            {
                Up = source.Bandwidth.Up,
                Down = source.Bandwidth.Down
            };
        }

        return target;
    }
}
