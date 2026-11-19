using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace DeployHysteria.UI.Services;

public class LocalizationService : ILocalizationService
{
    private AppLanguage _currentLanguage;

    public event PropertyChangedEventHandler? PropertyChanged;

    public LocalizationService()
    {
        // Detect system language
        var sysCulture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        _currentLanguage = sysCulture.Equals("ru", StringComparison.OrdinalIgnoreCase)
            ? AppLanguage.Russian
            : AppLanguage.English;
    }

    public AppLanguage CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            if (_currentLanguage != value)
            {
                _currentLanguage = value;
                OnPropertyChanged(string.Empty); // refresh all bindings
                OnPropertyChanged(nameof(CurrentLanguage));
                OnPropertyChanged("Item[]");
            }
        }
    }

    public string this[string key] => GetString(key);

    public string GetString(string key)
    {
        var dict = _currentLanguage == AppLanguage.Russian ? RussianDict : EnglishDict;
        return dict.TryGetValue(key, out var val) ? val : key;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private static readonly Dictionary<string, string> RussianDict = new(StringComparer.OrdinalIgnoreCase)
    {
        ["AppTitle"] = "DeployHysteria 2",
        ["AppSubtitle"] = "Деплой и управление сервером Hysteria 2",
        ["TabDeploy"] = "Развертывание",
        ["TabManage"] = "Управление сервером",
        ["ModeSimple"] = "Простой режим",
        ["ModeAdvanced"] = "Продвинутый режим",
        ["ModeDescriptionSimple"] = "Базовые параметры для быстрой настройки за пару минут",
        ["ModeDescriptionAdvanced"] = "Полный контроль: порты, фаервол, fail2ban, маскировка, ACL и лимиты",

        // Server Connection
        ["ServerConnectionTitle"] = "1. Параметры подключения к серверу",
        ["HostLabel"] = "IP / Хост сервера",
        ["HostWatermark"] = "например: 194.87.12.34 или vps.example.com",
        ["HostTooltip"] = "IP-адрес или домен целевого сервера на базе Debian / Ubuntu",
        ["SshPortLabel"] = "SSH Порт",
        ["SshPortWatermark"] = "22",
        ["SshPortTooltip"] = "Порт для SSH-подключения к серверу (обычно 22)",
        ["UsernameLabel"] = "Пользователь",
        ["UsernameWatermark"] = "root",
        ["UsernameTooltip"] = "Имя пользователя сервера с root или sudo правами",
        ["AuthTypeLabel"] = "Метод авторизации",
        ["AuthTypePassword"] = "Пароль",
        ["AuthTypeKey"] = "SSH-ключ",
        ["PasswordLabel"] = "Пароль SSH",
        ["PasswordWatermark"] = "Введите пароль сервера",
        ["PasswordTooltip"] = "Пароль учетной записи на сервере",
        ["SshKeyLabel"] = "Приватный SSH-ключ",
        ["SshKeyWatermark"] = "-----BEGIN OPENSSH PRIVATE KEY-----...",
        ["SshKeyTooltip"] = "Содержимое приватного ключа RSA/ED25519",
        ["SshKeyPassphraseLabel"] = "Passphrase ключа (опционально)",
        ["SshKeyPassphraseWatermark"] = "Пароль от ключа",

        // Hysteria Config
        ["HysteriaConfigTitle"] = "2. Конфигурация Hysteria 2",
        ["DomainLabel"] = "Доменное имя (ACME / SNI)",
        ["DomainWatermark"] = "vpn.example.com",
        ["DomainTooltip"] = "Домен должен быть направлен A-записью на IP сервера для выпуска бесплатного Let's Encrypt TLS-сертификата",
        ["EmailLabel"] = "Email для сертификата",
        ["EmailWatermark"] = "admin@example.com",
        ["EmailTooltip"] = "Контактный email для регистрации в сервисе Let's Encrypt",
        ["ListenPortLabel"] = "Порт прослушивания Hysteria",
        ["ListenPortWatermark"] = "443",
        ["ListenPortTooltip"] = "Порт Hysteria 2 (по умолчанию 443 для максимальной маскировки)",
        ["ClientPasswordLabel"] = "Пароль клиента Hysteria",
        ["ClientPasswordWatermark"] = "Секретный пароль",
        ["ClientPasswordTooltip"] = "Пароль, по которому клиенты будут подключаться к серверу",
        ["GenerateButton"] = "Сгенерировать",
        
        // Obfuscation
        ["ObfsTitle"] = "Обфускация трафика (Salamander)",
        ["EnableObfsLabel"] = "Включить Salamander обфускацию",
        ["ObfsPasswordLabel"] = "Пароль обфускации",
        ["ObfsPasswordWatermark"] = "Пароль обфускации",
        ["ObfsTooltip"] = "Маскирует протокол UDP/QUIC под случайный энтропийный шум для обхода блокировок",

        // Masquerade
        ["MasqueradeTitle"] = "Маскировка (Masquerade)",
        ["MasqueradeTypeLabel"] = "Тип маскировки",
        ["MasqueradeTypeFile"] = "Файловый сайт (HTML)",
        ["MasqueradeTypeProxy"] = "Проксирование (Reverse Proxy)",
        ["MasqueradeDirLabel"] = "Каталог сайта на сервере",
        ["MasqueradeDirWatermark"] = "/var/www/masq",
        ["MasqueradeProxyUrlLabel"] = "URL для проксирования",
        ["MasqueradeProxyUrlWatermark"] = "https://example.com",
        ["MasqueradeTooltip"] = "Отдает настоящий сайт, если кто-то открывает сервер через браузер по HTTPS",

        // Bandwidth
        ["BandwidthTitle"] = "Ограничение скорости (Bandwidth)",
        ["BandwidthUpLabel"] = "Исходящая скорость (Upload)",
        ["BandwidthUpWatermark"] = "например: 100 mbps",
        ["BandwidthDownLabel"] = "Входящая скорость (Download)",
        ["BandwidthDownWatermark"] = "например: 100 mbps",
        ["BandwidthTooltip"] = "Ограничение скорости на сервере (например: 100 mbps, 50 mbps). Оставьте пустым, если без лимита",

        // Security
        ["SecurityTitle"] = "Безопасность и фаервол",
        ["EnableUfwLabel"] = "Настроить фаервол UFW (открыть SSH и 443 TCP/UDP)",
        ["EnableFail2BanLabel"] = "Включить защиту Fail2Ban (3 попытки, бан 10 часов)",
        ["Fail2BanMaxRetryLabel"] = "Макс. попыток",
        ["Fail2BanBanTimeLabel"] = "Время бана (часы)",

        // Deployment Actions
        ["StartDeployButton"] = "Развернуть Hysteria 2 на сервере",
        ["DeployingButton"] = "Выполняется развертывание...",
        ["DeploymentLogsTitle"] = "Ход выполнения и лог",
        ["VerifyDnsLabel"] = "Проверять DNS-запись домена перед стартом",
        ["CheckPortLabel"] = "Проверять свободность порта 443 на сервере",

        // Management View
        ["ConnectButton"] = "Подключиться к серверу",
        ["ConnectingButton"] = "Подключение...",
        ["DisconnectButton"] = "Отключиться",
        ["ServerStatusTitle"] = "Состояние сервера",
        ["ContainerStatusLabel"] = "Контейнер Hysteria 2:",
        ["UfwStatusLabel"] = "Фаервол UFW:",
        ["Fail2BanStatusLabel"] = "Защита Fail2Ban:",
        ["StatusActive"] = "Активен",
        ["StatusInactive"] = "Не активен",
        ["StatusRunning"] = "Работает",
        ["StatusStopped"] = "Остановлен",
        ["UsersSectionTitle"] = "Пользователи Hysteria",
        ["NewUsernameWatermark"] = "Имя пользователя (логин)",
        ["NewUserPasswordWatermark"] = "Пароль",
        ["AddUserButton"] = "Добавить пользователя",
        ["DeleteUserButton"] = "Удалить",
        ["ShowQrButton"] = "QR-код и ссылка",
        ["CopyLinkButton"] = "Скопировать ссылку",
        ["SaveQrImageButton"] = "Сохранить QR-код (.png)",
        ["ExportTxtButton"] = "Экспорт настроек (.txt)",
        ["RefreshStatusButton"] = "Обновить статус",
        ["RestartServiceButton"] = "Перезапустить Hysteria",
        ["ContainerLogsTitle"] = "Логи контейнера",
        ["RefreshLogsButton"] = "Обновить логи",

        // Dialogs / Notifications
        ["DialogTitleQr"] = "Настройки подключения для клиента",
        ["CloseButton"] = "Закрыть",
        ["CopiedToClipboard"] = "Ссылка скопирована в буфер обмена!",
        ["SavedToFile"] = "Файл успешно сохранен!",
        ["ErrorTitle"] = "Ошибка",
        ["SuccessTitle"] = "Успешно"
    };

    private static readonly Dictionary<string, string> EnglishDict = new(StringComparer.OrdinalIgnoreCase)
    {
        ["AppTitle"] = "DeployHysteria 2",
        ["AppSubtitle"] = "Hysteria 2 Server Deployment & Management",
        ["TabDeploy"] = "Deploy",
        ["TabManage"] = "Manage Server",
        ["ModeSimple"] = "Simple Mode",
        ["ModeAdvanced"] = "Advanced Mode",
        ["ModeDescriptionSimple"] = "Essential parameters for a fast 2-minute setup",
        ["ModeDescriptionAdvanced"] = "Full control: ports, firewall, fail2ban, masquerade, ACL, and limits",

        // Server Connection
        ["ServerConnectionTitle"] = "1. Server Connection Parameters",
        ["HostLabel"] = "Server IP / Host",
        ["HostWatermark"] = "e.g. 194.87.12.34 or vps.example.com",
        ["HostTooltip"] = "IP address or domain of target Linux server (Debian / Ubuntu)",
        ["SshPortLabel"] = "SSH Port",
        ["SshPortWatermark"] = "22",
        ["SshPortTooltip"] = "Port for SSH connection (usually 22)",
        ["UsernameLabel"] = "Username",
        ["UsernameWatermark"] = "root",
        ["UsernameTooltip"] = "Server user account with root or sudo privileges",
        ["AuthTypeLabel"] = "Authentication Method",
        ["AuthTypePassword"] = "Password",
        ["AuthTypeKey"] = "SSH Key",
        ["PasswordLabel"] = "SSH Password",
        ["PasswordWatermark"] = "Enter server password",
        ["PasswordTooltip"] = "Account password on remote server",
        ["SshKeyLabel"] = "Private SSH Key",
        ["SshKeyWatermark"] = "-----BEGIN OPENSSH PRIVATE KEY-----...",
        ["SshKeyTooltip"] = "RSA/ED25519 private key content",
        ["SshKeyPassphraseLabel"] = "Key Passphrase (optional)",
        ["SshKeyPassphraseWatermark"] = "Key passphrase",

        // Hysteria Config
        ["HysteriaConfigTitle"] = "2. Hysteria 2 Configuration",
        ["DomainLabel"] = "Domain Name (ACME / SNI)",
        ["DomainWatermark"] = "vpn.example.com",
        ["DomainTooltip"] = "Domain must point to server's IP via A record to obtain a free Let's Encrypt TLS certificate",
        ["EmailLabel"] = "Certificate Email",
        ["EmailWatermark"] = "admin@example.com",
        ["EmailTooltip"] = "Contact email for Let's Encrypt ACME registration",
        ["ListenPortLabel"] = "Hysteria Listen Port",
        ["ListenPortWatermark"] = "443",
        ["ListenPortTooltip"] = "Port for Hysteria 2 (defaults to 443 for maximum stealth)",
        ["ClientPasswordLabel"] = "Client Hysteria Password",
        ["ClientPasswordWatermark"] = "Secret client password",
        ["ClientPasswordTooltip"] = "Password clients will use to authenticate with Hysteria 2",
        ["GenerateButton"] = "Generate",

        // Obfuscation
        ["ObfsTitle"] = "Traffic Obfuscation (Salamander)",
        ["EnableObfsLabel"] = "Enable Salamander Obfuscation",
        ["ObfsPasswordLabel"] = "Obfuscation Password",
        ["ObfsPasswordWatermark"] = "Obfuscation password",
        ["ObfsTooltip"] = "Masks UDP/QUIC protocol into random entropy noise to bypass DPI inspection",

        // Masquerade
        ["MasqueradeTitle"] = "Masquerade",
        ["MasqueradeTypeLabel"] = "Masquerade Type",
        ["MasqueradeTypeFile"] = "Static Website (HTML)",
        ["MasqueradeTypeProxy"] = "Reverse Proxy",
        ["MasqueradeDirLabel"] = "Website Directory on Server",
        ["MasqueradeDirWatermark"] = "/var/www/masq",
        ["MasqueradeProxyUrlLabel"] = "Proxy Target URL",
        ["MasqueradeProxyUrlWatermark"] = "https://example.com",
        ["MasqueradeTooltip"] = "Serves a regular website when visited via web browser over HTTPS",

        // Bandwidth
        ["BandwidthTitle"] = "Speed Limits (Bandwidth)",
        ["BandwidthUpLabel"] = "Upload Limit",
        ["BandwidthUpWatermark"] = "e.g. 100 mbps",
        ["BandwidthDownLabel"] = "Download Limit",
        ["BandwidthDownWatermark"] = "e.g. 100 mbps",
        ["BandwidthTooltip"] = "Speed throttling on server (e.g. 100 mbps). Leave empty for unlimited",

        // Security
        ["SecurityTitle"] = "Server Security & Firewall",
        ["EnableUfwLabel"] = "Configure UFW firewall (open SSH & 443 TCP/UDP)",
        ["EnableFail2BanLabel"] = "Enable Fail2Ban protection (3 retries, 10h ban)",
        ["Fail2BanMaxRetryLabel"] = "Max Retries",
        ["Fail2BanBanTimeLabel"] = "Ban Duration (hours)",

        // Deployment Actions
        ["StartDeployButton"] = "Deploy Hysteria 2 to Server",
        ["DeployingButton"] = "Deploying...",
        ["DeploymentLogsTitle"] = "Deployment Progress & Logs",
        ["VerifyDnsLabel"] = "Verify domain DNS record points to server IP",
        ["CheckPortLabel"] = "Check that port 443 is free on server",

        // Management View
        ["ConnectButton"] = "Connect to Server",
        ["ConnectingButton"] = "Connecting...",
        ["DisconnectButton"] = "Disconnect",
        ["ServerStatusTitle"] = "Server Status",
        ["ContainerStatusLabel"] = "Hysteria 2 Container:",
        ["UfwStatusLabel"] = "UFW Firewall:",
        ["Fail2BanStatusLabel"] = "Fail2Ban Service:",
        ["StatusActive"] = "Active",
        ["StatusInactive"] = "Inactive",
        ["StatusRunning"] = "Running",
        ["StatusStopped"] = "Stopped",
        ["UsersSectionTitle"] = "Hysteria Users",
        ["NewUsernameWatermark"] = "Username",
        ["NewUserPasswordWatermark"] = "Password",
        ["AddUserButton"] = "Add User",
        ["DeleteUserButton"] = "Delete",
        ["ShowQrButton"] = "QR Code & Link",
        ["CopyLinkButton"] = "Copy Link",
        ["SaveQrImageButton"] = "Save QR Image (.png)",
        ["ExportTxtButton"] = "Export Config (.txt)",
        ["RefreshStatusButton"] = "Refresh Status",
        ["RestartServiceButton"] = "Restart Hysteria",
        ["ContainerLogsTitle"] = "Container Logs",
        ["RefreshLogsButton"] = "Refresh Logs",

        // Dialogs / Notifications
        ["DialogTitleQr"] = "Client Connection Profile",
        ["CloseButton"] = "Close",
        ["CopiedToClipboard"] = "Link copied to clipboard!",
        ["SavedToFile"] = "File saved successfully!",
        ["ErrorTitle"] = "Error",
        ["SuccessTitle"] = "Success"
    };
}
