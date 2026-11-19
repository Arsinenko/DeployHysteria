using DeployHysteria.UI.Services;
using FluentAssertions;
using Xunit;

namespace DeployHysteria.UI.Tests;

public class LocalizationServiceTests
{
    [Fact]
    public void GetString_Russian_ShouldReturnRussianText()
    {
        var loc = new LocalizationService
        {
            CurrentLanguage = AppLanguage.Russian
        };

        loc["TabDeploy"].Should().Be("Развертывание");
        loc["TabManage"].Should().Be("Управление сервером");
        loc["ModeSimple"].Should().Be("Простой режим");
        loc["ModeAdvanced"].Should().Be("Продвинутый режим");
    }

    [Fact]
    public void GetString_English_ShouldReturnEnglishText()
    {
        var loc = new LocalizationService
        {
            CurrentLanguage = AppLanguage.English
        };

        loc["TabDeploy"].Should().Be("Deploy");
        loc["TabManage"].Should().Be("Manage Server");
        loc["ModeSimple"].Should().Be("Simple Mode");
        loc["ModeAdvanced"].Should().Be("Advanced Mode");
    }

    [Fact]
    public void PropertyChanged_FiredOnLanguageSwitch()
    {
        var loc = new LocalizationService
        {
            CurrentLanguage = AppLanguage.English
        };

        bool eventFired = false;
        loc.PropertyChanged += (_, _) => eventFired = true;

        loc.CurrentLanguage = AppLanguage.Russian;

        eventFired.Should().BeTrue();
        loc["TabDeploy"].Should().Be("Развертывание");
    }
}
