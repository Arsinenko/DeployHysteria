using System;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeployHysteria.Core.Management;
using DeployHysteria.Core.Models.Connection;
using DeployHysteria.Core.Models.Hysteria;
using DeployHysteria.UI.Services;

namespace DeployHysteria.UI.ViewModels;

public partial class ManageViewModel : ViewModelBase
{
    private readonly IHysteriaManager _manager;
    private readonly IHysteriaClientConfigService _clientConfigService;
    private readonly IClipboardService _clipboardService;
    private readonly IFileSaveService _fileSaveService;
    private readonly ILocalizationService _loc;

    // Connection
    [ObservableProperty]
    private string _host = string.Empty;

    [ObservableProperty]
    private int _sshPort = 22;

    [ObservableProperty]
    private string _username = "root";

    [ObservableProperty]
    private int _authTypeIndex = 0; // 0 = Password, 1 = Key

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _sshPrivateKey = string.Empty;

    [ObservableProperty]
    private string _remoteDirectory = "/opt/hysteria2";

    // State
    [ObservableProperty]
    private bool _isConnected = false;

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    // Server Info
    [ObservableProperty]
    private bool _containerRunning;

    [ObservableProperty]
    private string _containerStatusText = string.Empty;

    [ObservableProperty]
    private string _ufwStatusText = string.Empty;

    [ObservableProperty]
    private string _fail2BanStatusText = string.Empty;

    [ObservableProperty]
    private string _containerLogs = string.Empty;

    // Config Cache
    [ObservableProperty]
    private HysteriaConfig? _currentConfig;

    // Users
    [ObservableProperty]
    private ObservableCollection<UserItemViewModel> _users = [];

    [ObservableProperty]
    private string _newUsername = string.Empty;

    [ObservableProperty]
    private string _newUserPassword = string.Empty;

    // Dialog
    [ObservableProperty]
    private QrCodeDialogViewModel? _qrDialog;

    public ILocalizationService Loc => _loc;

    public ManageViewModel(
        IHysteriaManager manager,
        IHysteriaClientConfigService clientConfigService,
        IClipboardService clipboardService,
        IFileSaveService fileSaveService,
        ILocalizationService loc)
    {
        _manager = manager;
        _clientConfigService = clientConfigService;
        _clipboardService = clipboardService;
        _fileSaveService = fileSaveService;
        _loc = loc;

        GenerateNewUserPassword();
    }

    [RelayCommand]
    public void SelectPasswordAuth() => AuthTypeIndex = 0;

    [RelayCommand]
    public void SelectKeyAuth() => AuthTypeIndex = 1;

    [RelayCommand]
    public void GenerateNewUserPassword()
    {
        NewUserPassword = GenerateRandomPassword(18);
    }

    private ServerConnectionInfo CreateConnectionInfo()
    {
        return new ServerConnectionInfo
        {
            Host = Host.Trim(),
            Port = SshPort,
            Username = Username.Trim(),
            Password = AuthTypeIndex == 0 ? Password : null,
            SshPrivateKey = AuthTypeIndex == 1 ? SshPrivateKey : null
        };
    }

    [RelayCommand]
    public async Task ConnectAsync()
    {
        ErrorMessage = string.Empty;
        StatusMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(Host))
        {
            ErrorMessage = "Host cannot be empty.";
            return;
        }

        if (AuthTypeIndex == 0 && string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Password is required.";
            return;
        }

        if (AuthTypeIndex == 1 && string.IsNullOrWhiteSpace(SshPrivateKey))
        {
            ErrorMessage = "SSH Private Key is required.";
            return;
        }

        IsLoading = true;
        try
        {
            var conn = CreateConnectionInfo();
            CurrentConfig = await _manager.GetCurrentConfigAsync(conn, RemoteDirectory);
            await RefreshStatusAsync();
            await RefreshUsersAsync();

            IsConnected = true;
            StatusMessage = "Connected to server successfully.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Connection failed: {ex.Message}";
            IsConnected = false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void Disconnect()
    {
        IsConnected = false;
        CurrentConfig = null;
        Users.Clear();
        ContainerLogs = string.Empty;
        StatusMessage = "Disconnected.";
    }

    [RelayCommand]
    public async Task RefreshStatusAsync()
    {
        if (string.IsNullOrWhiteSpace(Host)) return;

        IsLoading = true;
        try
        {
            var conn = CreateConnectionInfo();
            var status = await _manager.GetStatusAsync(conn, RemoteDirectory);

            ContainerRunning = status.ContainerRunning;
            ContainerStatusText = status.ContainerStatus;
            UfwStatusText = status.UfwStatus;
            Fail2BanStatusText = status.Fail2BanStatus;
            ContainerLogs = status.RecentLogs;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to get status: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task RefreshUsersAsync()
    {
        if (string.IsNullOrWhiteSpace(Host)) return;

        try
        {
            var conn = CreateConnectionInfo();
            var userList = await _manager.GetUsersAsync(conn, RemoteDirectory);

            Users.Clear();
            foreach (var user in userList)
            {
                Users.Add(new UserItemViewModel(
                    user.Username,
                    user.Password,
                    ShowUserQrAsync,
                    DeleteUserAsync));
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load users: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task AddUserAsync()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(NewUsername))
        {
            ErrorMessage = "Username cannot be empty.";
            return;
        }

        if (string.IsNullOrWhiteSpace(NewUserPassword))
        {
            ErrorMessage = "Password cannot be empty.";
            return;
        }

        IsLoading = true;
        try
        {
            var conn = CreateConnectionInfo();
            await _manager.AddOrUpdateUserAsync(conn, NewUsername.Trim(), NewUserPassword.Trim(), RemoteDirectory);

            StatusMessage = $"User '{NewUsername}' added successfully.";
            NewUsername = string.Empty;
            GenerateNewUserPassword();

            // Refresh config and users
            CurrentConfig = await _manager.GetCurrentConfigAsync(conn, RemoteDirectory);
            await RefreshUsersAsync();
            await RefreshStatusAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to add user: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task DeleteUserAsync(UserItemViewModel userVm)
    {
        IsLoading = true;
        try
        {
            var conn = CreateConnectionInfo();
            var removed = await _manager.RemoveUserAsync(conn, userVm.Username, RemoteDirectory);
            if (removed)
            {
                StatusMessage = $"User '{userVm.Username}' removed.";
                CurrentConfig = await _manager.GetCurrentConfigAsync(conn, RemoteDirectory);
                await RefreshUsersAsync();
                await RefreshStatusAsync();
            }
            else
            {
                ErrorMessage = $"User '{userVm.Username}' could not be removed.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to remove user: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public Task ShowUserQrAsync(UserItemViewModel userVm)
    {
        if (CurrentConfig == null) return Task.CompletedTask;

        var clientInfo = _clientConfigService.CreateFromConfig(CurrentConfig, userVm.Username, userVm.Password, Host);
        QrDialog = new QrCodeDialogViewModel(clientInfo, _clientConfigService, _clipboardService, _fileSaveService, _loc);
        return Task.CompletedTask;
    }

    [RelayCommand]
    public async Task RestartServiceAsync()
    {
        IsLoading = true;
        try
        {
            var conn = CreateConnectionInfo();
            await _manager.RestartServiceAsync(conn, RemoteDirectory);
            StatusMessage = "Service restarted successfully.";
            await RefreshStatusAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to restart service: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task RefreshLogsAsync()
    {
        IsLoading = true;
        try
        {
            var conn = CreateConnectionInfo();
            ContainerLogs = await _manager.GetLogsAsync(conn, lines: 100, RemoteDirectory);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to fetch logs: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static string GenerateRandomPassword(int length = 18)
    {
        const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890-_";
        var bytes = RandomNumberGenerator.GetBytes(length);
        var result = new char[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = validChars[bytes[i] % validChars.Length];
        }
        return new string(result);
    }
}
