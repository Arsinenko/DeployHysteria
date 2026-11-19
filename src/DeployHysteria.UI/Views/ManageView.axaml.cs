using Avalonia.Controls;
using DeployHysteria.UI.Services;

namespace DeployHysteria.UI.Views;

public partial class ManageView : UserControl
{
    public ManageView()
    {
        InitializeComponent();
        ResponsiveHelper.Attach(this, 560);
    }
}
