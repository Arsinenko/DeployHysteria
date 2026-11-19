using DeployHysteria.Core.Models.Hysteria;

namespace DeployHysteria.Core.Serialization;

public interface IHysteriaConfigSerializer
{
    string Serialize(HysteriaConfig config);
    HysteriaConfig Deserialize(string yaml);
}
