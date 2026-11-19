using DeployHysteria.Core.Deployment;
using DeployHysteria.Core.Management;
using DeployHysteria.Core.Network;
using DeployHysteria.Core.Provisioning;
using DeployHysteria.Core.Serialization;
using DeployHysteria.Core.Ssh;
using Microsoft.Extensions.DependencyInjection;

namespace DeployHysteria.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHysteriaDeployer(this IServiceCollection services)
    {
        services.AddSingleton<IHysteriaConfigSerializer, HysteriaConfigSerializer>();
        services.AddSingleton<IDockerComposeGenerator, DockerComposeGenerator>();
        services.AddSingleton<IDnsResolver, SystemDnsResolver>();
        services.AddSingleton<IPortChecker, RemotePortChecker>();
        services.AddSingleton<ISshClientFactory, SshNetClientFactory>();
        services.AddTransient<IServerProvisioner, LinuxServerProvisioner>();
        services.AddTransient<IHysteriaDeployer, HysteriaDeployer>();
        services.AddTransient<IHysteriaManager, HysteriaManager>();

        return services;
    }
}
