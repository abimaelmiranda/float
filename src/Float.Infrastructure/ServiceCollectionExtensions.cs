using Float.Core.Abstractions.Services;
using Float.Core.Enums;
using Float.Infrastructure.Common;
using Float.Infrastructure.Engines.AppleContainers;
using Float.Infrastructure.Engines.Docker;
using Float.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Float.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFloatInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IProcessHost, ProcessHost>();
        services.AddSingleton<IFileSystemService, FileSystemService>();

        services.AddKeyedSingleton<IEngineProvisioner, AppleContainersEngineProvisioner>(ContainerEngine.AppleContainers);
        services.AddKeyedSingleton<IContainerReader, AppleContainerReader>(ContainerEngine.AppleContainers);
        services.AddKeyedSingleton<IContainerCreator, AppleContainerCreator>(ContainerEngine.AppleContainers);
        services.AddKeyedSingleton<IContainerVolumeCreator, AppleContainerVolumeCreator>(ContainerEngine.AppleContainers);
        services.AddKeyedSingleton<IContainerVolumeLifecycle, AppleContainerVolumeLifecycle>(ContainerEngine.AppleContainers);
        services.AddKeyedSingleton<IContainerImageLifecycle, AppleContainerImageLifecycle>(ContainerEngine.AppleContainers);
        services.AddKeyedSingleton<IContainerLifecycle, AppleContainerLifecycle>(ContainerEngine.AppleContainers);

        services.AddKeyedSingleton<IEngineProvisioner, DockerContainerEngineProvisioner>(ContainerEngine.Docker);
        services.AddKeyedSingleton<IContainerReader, DockerReader>(ContainerEngine.Docker);
        services.AddKeyedSingleton<IContainerLifecycle, DockerContainerLifecycle>(ContainerEngine.Docker);

        // Active engine forwarding — change the key here to switch engines
        services.AddSingleton<IEngineProvisioner>(sp =>
            sp.GetRequiredKeyedService<IEngineProvisioner>(ContainerEngine.AppleContainers));
        services.AddSingleton<IContainerReader>(sp =>
            sp.GetRequiredKeyedService<IContainerReader>(ContainerEngine.AppleContainers));
        services.AddSingleton<IContainerCreator>(sp =>
            sp.GetRequiredKeyedService<IContainerCreator>(ContainerEngine.AppleContainers));
        services.AddSingleton<IContainerVolumeCreator>(sp =>
            sp.GetRequiredKeyedService<IContainerVolumeCreator>(ContainerEngine.AppleContainers));
        services.AddSingleton<IContainerVolumeLifecycle>(sp =>
            sp.GetRequiredKeyedService<IContainerVolumeLifecycle>(ContainerEngine.AppleContainers));
        services.AddSingleton<IContainerImageLifecycle>(sp =>
            sp.GetRequiredKeyedService<IContainerImageLifecycle>(ContainerEngine.AppleContainers));
        services.AddSingleton<IContainerLifecycle>(sp =>
            sp.GetRequiredKeyedService<IContainerLifecycle>(ContainerEngine.AppleContainers));

        return services;
    }
}
