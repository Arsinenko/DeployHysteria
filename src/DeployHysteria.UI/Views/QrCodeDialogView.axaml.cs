using Avalonia.Controls;
using DeployHysteria.UI.Services;

namespace DeployHysteria.UI.Views;

public partial class QrCodeDialogView : UserControl
{
    public QrCodeDialogView()
    {
        InitializeComponent();
        ResponsiveHelper.Attach(this, 520);
    }
}
