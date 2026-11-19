using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeployHysteria.UI.Services;

namespace DeployHysteria.UI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly ILocalizationService _loc;

    [ObservableProperty]
    private int _selectedTabIndex = 0;

    [ObservableProperty]
    private DeployViewModel _deploy;

    [ObservableProperty]
    private ManageViewModel _manage;

    public ILocalizationService Loc => _loc;

    public string CurrentLanguageCode => _loc.CurrentLanguage == AppLanguage.Russian ? "RU" : "EN";

    public MainViewModel(
        DeployViewModel deploy,
        ManageViewModel manage,
        ILocalizationService loc)
    {
        _deploy = deploy;
        _manage = manage;
        _loc = loc;

        _deploy.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(DeployViewModel.QrDialog))
            {
                OnPropertyChanged(nameof(ActiveQrDialog));
            }
        };

        _manage.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ManageViewModel.QrDialog))
            {
                OnPropertyChanged(nameof(ActiveQrDialog));
            }
        };
    }

    public QrCodeDialogViewModel? ActiveQrDialog
    {
        get
        {
            if (Deploy.QrDialog?.IsOpen == true) return Deploy.QrDialog;
            if (Manage.QrDialog?.IsOpen == true) return Manage.QrDialog;
            return null;
        }
    }

    [RelayCommand]
    public void SetDeployTab() => SelectedTabIndex = 0;

    [RelayCommand]
    public void SetManageTab() => SelectedTabIndex = 1;

    [RelayCommand]
    public void ToggleLanguage()
    {
        _loc.CurrentLanguage = _loc.CurrentLanguage == AppLanguage.Russian
            ? AppLanguage.English
            : AppLanguage.Russian;

        OnPropertyChanged(nameof(CurrentLanguageCode));
    }
}
