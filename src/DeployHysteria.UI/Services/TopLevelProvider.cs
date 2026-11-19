using Avalonia;
using Avalonia.Controls;

namespace DeployHysteria.UI.Services;

public static class TopLevelProvider
{
    private static WeakReference<Visual>? _rootVisual;

    public static void RegisterRootVisual(Visual visual)
    {
        _rootVisual = new WeakReference<Visual>(visual);
    }

    public static TopLevel? GetTopLevel()
    {
        if (_rootVisual != null && _rootVisual.TryGetTarget(out var visual))
        {
            var topLevel = TopLevel.GetTopLevel(visual);
            if (topLevel != null)
            {
                return topLevel;
            }
        }

        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }

        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.ISingleViewApplicationLifetime singleView)
        {
            return TopLevel.GetTopLevel(singleView.MainView);
        }

        return null;
    }
}
