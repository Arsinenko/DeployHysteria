using System;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeployHysteria.Core.Deployment;
using DeployHysteria.Core.Models.Connection;
using DeployHysteria.Core.Models.Deployment;
using DeployHysteria.Core.Models.Hysteria;
using DeployHysteria.UI.Services;

namespace DeployHysteria.UI.ViewModels;

public partial class DeployViewModel : ViewModelBase
{
    private readonly IHysteriaDeployer _deployer;
    private readonly IHysteriaClientConfigService _clientConfigService;
    private readonly IClipboardService _clipboardService;
    private readonly IFileSaveService _fileSaveService;
    private readonly ILocalizationService _loc;

    [ObservableProperty]
    private bool _isAdvancedMode = false;

    // Connection
    [ObservableProperty]
    private string _host = string.Empty;

    [ObservableProperty]
    private int _sshPort = 22;

    [ObservableProperty]
    private string _username = "root";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPasswordAuth))]
    [NotifyPropertyChangedFor(nameof(IsKeyAuth))]
    private int _authTypeIndex = 0; // 0 = Password, 1 = Key

    public bool IsPasswordAuth => AuthTypeIndex == 0;
    public bool IsKeyAuth => AuthTypeIndex == 1;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _sshPrivateKey = string.Empty;

    [ObservableProperty]
    private string _sshPrivateKeyPassphrase = string.Empty;

    // Hysteria
    [ObservableProperty]
    private string _domain = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private int _listenPort = 443;

    [ObservableProperty]
    private string _clientPassword = string.Empty;

    [ObservableProperty]
    private bool _enableObfs = true;

    [ObservableProperty]
    private string _obfsPassword = string.Empty;

    // Advanced options
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFileMasquerade))]
    [NotifyPropertyChangedFor(nameof(IsProxyMasquerade))]
    private int _masqueradeTypeIndex = 0; // 0 = File, 1 = Proxy

    public bool IsFileMasquerade => MasqueradeTypeIndex == 0;
    public bool IsProxyMasquerade => MasqueradeTypeIndex == 1;

    [ObservableProperty]
    private string _masqueradeDir = "/var/www/masq";

    [ObservableProperty]
    private string _masqueradeProxyUrl = "https://bing.com";

    [ObservableProperty]
    private string _bandwidthUp = string.Empty;

    [ObservableProperty]
    private string _bandwidthDown = string.Empty;

    [ObservableProperty]
    private bool _enableUfw = true;

    [ObservableProperty]
    private bool _enableFail2Ban = true;

    [ObservableProperty]
    private int _fail2BanMaxRetry = 3;

    [ObservableProperty]
    private int _fail2BanBanTimeHours = 10;

    [ObservableProperty]
    private bool _verifyDns = true;

    [ObservableProperty]
    private bool _checkPort = true;

    // Status & Logs
    [ObservableProperty]
    private bool _isDeploying = false;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _deploymentLogs = [];

    [ObservableProperty]
    private bool _isSuccess = false;

    [ObservableProperty]
    private QrCodeDialogViewModel? _qrDialog;

    public ILocalizationService Loc => _loc;

    public DeployViewModel(
        IHysteriaDeployer deployer,
        IHysteriaClientConfigService clientConfigService,
        IClipboardService clipboardService,
        IFileSaveService fileSaveService,
        ILocalizationService loc)
    {
        _deployer = deployer;
        _clientConfigService = clientConfigService;
        _clipboardService = clipboardService;
        _fileSaveService = fileSaveService;
        _loc = loc;

        GeneratePasswords();
    }

    [RelayCommand]
    public void SelectPasswordAuth() => AuthTypeIndex = 0;

    [RelayCommand]
    public void SelectKeyAuth() => AuthTypeIndex = 1;

    [RelayCommand]
    public void GeneratePasswords()
    {
        ClientPassword = GenerateRandomPassword(18);
        ObfsPassword = GenerateRandomPassword(18);
    }

    [RelayCommand]
    public void ToggleMode()
    {
        IsAdvancedMode = !IsAdvancedMode;
    }

    [RelayCommand]
    public async Task StartDeployAsync()
    {
        ErrorMessage = string.Empty;
        StatusMessage = string.Empty;
        IsSuccess = false;

        if (string.IsNullOrWhiteSpace(Host))
        {
            ErrorMessage = "Host / IP cannot be empty.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Domain))
        {
            ErrorMessage = "Domain cannot be empty.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Email))
        {
            ErrorMessage = "ACME Email cannot be empty.";
            return;
        }

        if (string.IsNullOrWhiteSpace(ClientPassword))
        {
            ErrorMessage = "Client password cannot be empty.";
            return;
        }

        if (AuthTypeIndex == 0 && string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "SSH Password is required.";
            return;
        }

        if (AuthTypeIndex == 1 && string.IsNullOrWhiteSpace(SshPrivateKey))
        {
            ErrorMessage = "SSH Private Key is required.";
            return;
        }

        IsDeploying = true;
        DeploymentLogs.Clear();
        DeploymentLogs.Add($"[{DateTime.Now:HH:mm:ss}] Starting deployment process...");

        try
        {
            var serverConnection = new ServerConnectionInfo
            {
                Host = Host.Trim(),
                Port = SshPort,
                Username = Username.Trim(),
                Password = AuthTypeIndex == 0 ? Password : null,
                SshPrivateKey = AuthTypeIndex == 1 ? SshPrivateKey : null,
                SshPrivateKeyPassphrase = AuthTypeIndex == 1 && !string.IsNullOrWhiteSpace(SshPrivateKeyPassphrase)
                    ? SshPrivateKeyPassphrase
                    : null
            };

            var hysteriaConfig = new HysteriaConfig
            {
                Listen = $":{ListenPort}",
                Acme = new AcmeConfig
                {
                    Domains = [Domain.Trim()],
                    Email = Email.Trim(),
                    Dir = "/acme"
                },
                Acl = new AclConfig(),
                Auth = AuthConfig.FromSinglePassword(ClientPassword)
            };

            if (EnableObfs && !string.IsNullOrWhiteSpace(ObfsPassword))
            {
                hysteriaConfig.Obfs = new ObfsConfig
                {
                    Type = "salamander",
                    Salamander = new ObfsSalamanderConfig { Password = ObfsPassword }
                };
            }

            if (MasqueradeTypeIndex == 0)
            {
                hysteriaConfig.Masquerade = MasqueradeConfig.CreateFile(MasqueradeDir);
            }
            else
            {
                hysteriaConfig.Masquerade = MasqueradeConfig.CreateProxy(MasqueradeProxyUrl);
            }

            if (!string.IsNullOrWhiteSpace(BandwidthUp) || !string.IsNullOrWhiteSpace(BandwidthDown))
            {
                hysteriaConfig.Bandwidth = new BandwidthConfig
                {
                    Up = string.IsNullOrWhiteSpace(BandwidthUp) ? null : BandwidthUp.Trim(),
                    Down = string.IsNullOrWhiteSpace(BandwidthDown) ? null : BandwidthDown.Trim()
                };
            }

            var options = new DeployOptions
            {
                ServerConnection = serverConnection,
                HysteriaConfig = hysteriaConfig,
                RemoteDirectory = "/opt/hysteria2",
                UpdatePackages = true,
                InstallDocker = true,
                Firewall = new FirewallOptions
                {
                    Enabled = EnableUfw,
                    SshPort = SshPort,
                    HysteriaPort = ListenPort
                },
                Fail2Ban = new Fail2BanOptions
                {
                    Enabled = EnableFail2Ban,
                    MaxRetry = Fail2BanMaxRetry,
                    BanTime = TimeSpan.FromHours(Fail2BanBanTimeHours),
                    SshPort = SshPort
                },
                VerifyDns = VerifyDns,
                CheckPort = CheckPort
            };

            var result = await _deployer.DeployAsync(options, progress =>
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    DeploymentLogs.Add($"[{DateTime.Now:HH:mm:ss}] {progress}");
                });
            });

            if (result.Success)
            {
                IsSuccess = true;
                StatusMessage = "Hysteria 2 deployed and running successfully!";
                DeploymentLogs.Add($"[{DateTime.Now:HH:mm:ss}] SUCCESS: {result.Message}");

                if (!string.IsNullOrWhiteSpace(result.Logs))
                {
                    DeploymentLogs.Add($"[{DateTime.Now:HH:mm:ss}] Container Logs:\n{result.Logs}");
                }

                // Show QR code dialog automatically
                var clientInfo = _clientConfigService.CreateFromConfig(hysteriaConfig, "default", ClientPassword, Host);
                QrDialog = new QrCodeDialogViewModel(clientInfo, _clientConfigService, _clipboardService, _fileSaveService, _loc);
            }
            else
            {
                ErrorMessage = result.Message;
                DeploymentLogs.Add($"[{DateTime.Now:HH:mm:ss}] ERROR: {result.Message}");
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            DeploymentLogs.Add($"[{DateTime.Now:HH:mm:ss}] EXCEPTION: {ex.Message}");
        }
        finally
        {
            IsDeploying = false;
        }
    }

    [RelayCommand]
    public void OpenClientQr()
    {
        if (string.IsNullOrWhiteSpace(Domain) || string.IsNullOrWhiteSpace(ClientPassword))
        {
            return;
        }

        var clientInfo = new ClientConnectionInfo
        {
            ServerHost = Domain.Trim(),
            Port = ListenPort,
            Username = "default",
            Password = ClientPassword,
            Sni = Domain.Trim(),
            ObfsType = EnableObfs ? "salamander" : null,
            ObfsPassword = EnableObfs ? ObfsPassword : null,
            Insecure = false,
            Tag = $"default@{Domain.Trim()}"
        };

        QrDialog = new QrCodeDialogViewModel(clientInfo, _clientConfigService, _clipboardService, _fileSaveService, _loc);
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
