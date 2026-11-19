using DeployHysteria.Core.Deployment;
using DeployHysteria.Core.Management;
using DeployHysteria.UI.Services;
using DeployHysteria.UI.ViewModels;
using FluentAssertions;
using Moq;
using Xunit;

namespace DeployHysteria.UI.Tests;

public class MainViewModelTests
{
    private readonly MainViewModel _vm;

    public MainViewModelTests()
    {
        var loc = new LocalizationService();
        var configService = new Mock<IHysteriaClientConfigService>().Object;
        var clipboard = new Mock<IClipboardService>().Object;
        var fileSave = new Mock<IFileSaveService>().Object;

        var deployVm = new DeployViewModel(new Mock<IHysteriaDeployer>().Object, configService, clipboard, fileSave, loc);
        var manageVm = new ManageViewModel(new Mock<IHysteriaManager>().Object, configService, clipboard, fileSave, loc);

        _vm = new MainViewModel(deployVm, manageVm, loc);
    }

    [Fact]
    public void InitialState_DeployTabShouldBeSelected()
    {
        _vm.SelectedTabIndex.Should().Be(0);
        _vm.IsDeployTabSelected.Should().BeTrue();
        _vm.IsManageTabSelected.Should().BeFalse();
    }

    [Fact]
    public void SetManageTab_ShouldSwitchVisibilityProperties()
    {
        _vm.SetManageTabCommand.Execute(null);

        _vm.SelectedTabIndex.Should().Be(1);
        _vm.IsDeployTabSelected.Should().BeFalse();
        _vm.IsManageTabSelected.Should().BeTrue();

        _vm.SetDeployTabCommand.Execute(null);

        _vm.SelectedTabIndex.Should().Be(0);
        _vm.IsDeployTabSelected.Should().BeTrue();
        _vm.IsManageTabSelected.Should().BeFalse();
    }
}
