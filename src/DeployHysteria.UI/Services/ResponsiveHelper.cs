using Avalonia.Controls;

namespace DeployHysteria.UI.Services;

/// <summary>
/// Toggles the "compact" CSS class on a control when its width falls below a breakpoint.
/// </summary>
public static class ResponsiveHelper
{
    public const double DefaultBreakpoint = 600;

    public static void Attach(Control control, double breakpoint = DefaultBreakpoint)
    {
        UpdateClass(control, control.Bounds.Width, breakpoint);
        control.SizeChanged += (_, e) => UpdateClass(control, e.NewSize.Width, breakpoint);
    }

    private static void UpdateClass(Control control, double width, double breakpoint)
    {
        if (width <= 0)
        {
            return;
        }

        control.Classes.Set("compact", width < breakpoint);
    }
}
