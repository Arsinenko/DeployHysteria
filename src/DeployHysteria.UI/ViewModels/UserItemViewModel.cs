using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DeployHysteria.UI.ViewModels;

public partial class UserItemViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _username;

    [ObservableProperty]
    private string _password;

    private readonly Func<UserItemViewModel, Task> _onShowQr;
    private readonly Func<UserItemViewModel, Task> _onDelete;

    public UserItemViewModel(
        string username,
        string password,
        Func<UserItemViewModel, Task> onShowQr,
        Func<UserItemViewModel, Task> onDelete)
    {
        _username = username;
        _password = password;
        _onShowQr = onShowQr;
        _onDelete = onDelete;
    }

    [RelayCommand]
    private async Task ShowQrAsync() => await _onShowQr(this);

    [RelayCommand]
    private async Task DeleteAsync() => await _onDelete(this);
}
