using Avalonia.Controls;
using DeployHysteria.UI.Services;

namespace DeployHysteria.UI.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        TopLevelProvider.RegisterRootVisual(this);
        ResponsiveHelper.Attach(this, 720);
    }
}
