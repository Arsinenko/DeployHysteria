using Avalonia.Controls;
using DeployHysteria.UI.Services;

namespace DeployHysteria.UI.Views;

public partial class DeployView : UserControl
{
    public DeployView()
    {
        InitializeComponent();
        ResponsiveHelper.Attach(this, 560);
    }
}
