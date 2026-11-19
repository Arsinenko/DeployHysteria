using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DeployHysteria.Core.Extensions;
using DeployHysteria.UI.Services;
using DeployHysteria.UI.ViewModels;
using DeployHysteria.UI.Views;
using Microsoft.Extensions.DependencyInjection;

namespace DeployHysteria.UI;

public partial class App : Application
{
    private IServiceProvider? _services;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);
        _services = serviceCollection.BuildServiceProvider();

        var mainViewModel = _services.GetRequiredService<MainViewModel>();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainViewModel
            };
        }
        else if (ApplicationLifetime is IActivityApplicationLifetime singleViewFactoryApplicationLifetime)
        {
            singleViewFactoryApplicationLifetime.MainViewFactory = () => new MainView
            {
                DataContext = mainViewModel
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = mainViewModel
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Core services (Provisioning, SSH, Deployer, Manager, Serializers, DNS, PortChecker)
        services.AddHysteriaDeployer();

        // UI Services
        services.AddSingleton<ILocalizationService, LocalizationService>();
        services.AddSingleton<IHysteriaClientConfigService, HysteriaClientConfigService>();
        services.AddSingleton<IClipboardService, AvaloniaClipboardService>();
        services.AddSingleton<IFileSaveService, AvaloniaFileSaveService>();

        // ViewModels
        services.AddTransient<DeployViewModel>();
        services.AddTransient<ManageViewModel>();
        services.AddTransient<MainViewModel>();
    }
}
