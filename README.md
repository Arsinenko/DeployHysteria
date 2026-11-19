# DeployHysteria

Библиотека на **.NET 10 (C# 13/14)** для автоматизированного безопасного деплоя и управления **Hysteria 2** на удаленных Linux-серверах (Ubuntu/Debian) через SSH.

## Возможности

1. **Подготовка и провижининг сервера**:
   - Обновление системных пакетов (`apt-get update && apt-get upgrade -y`).
   - Автоматическая установка Docker и Docker Compose.
   - Настройка UFW фаервола: открытие портов SSH и 443 (TCP/UDP).
   - Опциональная настройка Fail2Ban: блокировка после 3 неудачных попыток на 10 часов (`bantime = 10h`, `findtime = 10m`, `maxretry = 3`).

2. **Предварительные проверки (Pre-flight checks)**:
   - Проверка DNS: валидация того, что домен из ACME действительно указывает на целевой IP сервера.
   - Проверка доступности порта (проверка, что порт 443 не занят другим процессом перед деплоем).

3. **Генерация конфигураций**:
   - `config.yaml` для Hysteria 2 (поддержка `listen`, `acme`, `acl`, `obfs salamander`, `auth` (пароль / multi-user userpass), `masquerade` (file / proxy), ограничения скорости `bandwidth`).
   - `docker-compose.yml` с необходимыми `cap_drop`, `cap_add: NET_BIND_SERVICE`, `no-new-privileges:true` и монтированием каталогов.

4. **Управление на работающем сервере**:
   - Добавление и удаление пользователей без ручного редактирования файлов.
   - Изменение любых параметров конфигурации с автоматическим обновлением и перезапуском сервиса.
   - Просмотр логов контейнера.
   - Мониторинг статуса (состояние контейнера, UFW, Fail2Ban).

5. **Безопасность и архитектура**:
   - Полное экранирование shell-аргументов для защиты от инъекций (`ShellSanitizer`).
   - Изоляция зависимостей через интерфейсы (`ISshClient`, `IDnsResolver`, `IPortChecker`, `IServerProvisioner`, `IHysteriaConfigSerializer`, `IDockerComposeGenerator`).
   - 100% тестируемость (60 юнит-тестов на xUnit + FluentAssertions + Moq).

## Структура проекта

```
DeployHysteria/
├── src/
│   ├── DeployHysteria.Core/             # Class Library (.NET 10): SSH, провижининг, сериализация, деплой, менеджер
│   ├── DeployHysteria.UI/               # Кроссплатформенный GUI (Avalonia 12, MVVM, Views, ViewModels, Localization)
│   ├── DeployHysteria.Desktop/          # Десктопный клиент (Windows, Linux, macOS)
│   └── DeployHysteria.Android/          # Мобильный клиент (Android)
└── tests/
    ├── DeployHysteria.Core.Tests/       # Юнит-тесты Core-библиотеки (60 тестов)
    └── DeployHysteria.UI.Tests/         # Юнит-тесты ViewModels и UI-сервисов (19 тестов)
```

## Возможности графического интерфейса (GUI)

- **Кроссплатформенность**: единая кодовая база на Avalonia UI для десктопа (Linux, Windows, macOS) и мобильных устройств (Android/iOS).
- **Автоматическая локализация**: определение языка системы (русский / английский) с возможностью ручного переключения в шапке приложения.
- **Поддержка тем**: автоматическое следование системной теме (светлая / темная).
- **Два режима настройки**:
  - **Простой режим (Simple Mode)**: минимально необходимое количество полей (IP, пароль, домен, email, пароль клиента) с автоматической генерацией надежных паролей и безопасными предустановленными настройками фаервола, Fail2Ban и маскировки.
  - **Продвинутый режим (Advanced Mode)**: полный контроль над портами (SSH, Hysteria), типами авторизации (пароль или SSH-ключ), методами маскировки (файловый сайт или reverse proxy), лимитами скорости (Upload/Download bandwidth), правилами UFW, Fail2Ban и DNS/Port предпроверками.
- **Подсказки и эргономика**: все поля снабжены контекстными подсказками (`PlaceholderText` и `ToolTip`), интуитивно понятная строгая карточная верстка без визуального шума.
- **Интеграция с клиентами Hysteria 2**:
  - Генерация стандартных ссылок `hysteria2://...` для вставки в клиенты v2rayN, Nekoray, Matsuri, NekoBox, Sing-box, Clash-verge.
  - Отображение четкого QR-кода прямо в приложении.
  - Экспорт и сохранение QR-кода в файл изображения `.png`.
  - Экспорт настроек подключения в текстовый файл `.txt` (с разбивкой по полям: Host, Port, SNI, Auth, Obfs).
- **Управление на сервере**:
  - Мониторинг статуса контейнера, UFW и Fail2Ban.
  - Добавление и удаление пользователей в один клик с генерацией QR-кодов.
  - Просмотр и обновление логов в реальном времени.
  - Перезапуск сервиса.

## Пример использования

### 1. Деплой Hysteria 2 на чистый сервер

```csharp
using DeployHysteria.Core.Deployment;
using DeployHysteria.Core.Extensions;
using DeployHysteria.Core.Models.Connection;
using DeployHysteria.Core.Models.Deployment;
using DeployHysteria.Core.Models.Hysteria;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddHysteriaDeployer();
var provider = services.BuildServiceProvider();

var deployer = provider.GetRequiredService<IHysteriaDeployer>();

var deployOptions = new DeployOptions
{
    ServerConnection = new ServerConnectionInfo
    {
        Host = "192.168.1.100",
        Port = 22,
        Username = "root",
        Password = "MySecretServerPassword"
    },
    HysteriaConfig = new HysteriaConfig
    {
        Listen = ":443",
        Acme = new AcmeConfig
        {
            Domains = ["vpn.example.com"],
            Email = "admin@example.com",
            Dir = "/acme"
        },
        Acl = new AclConfig
        {
            Inline =
            [
                "reject(geoip:ru)",
                "reject(geosite:category-ru)",
                "direct(all)"
            ]
        },
        Obfs = new ObfsConfig
        {
            Type = "salamander",
            Salamander = new ObfsSalamanderConfig { Password = "super secret salamander pass" }
        },
        Auth = AuthConfig.FromSinglePassword("initial-user-password"),
        Masquerade = MasqueradeConfig.CreateFile("/var/www/masq"),
        Bandwidth = new BandwidthConfig
        {
            Up = "100 mbps",
            Down = "100 mbps"
        }
    },
    Firewall = new FirewallOptions
    {
        Enabled = true,
        SshPort = 22,
        HysteriaPort = 443
    },
    Fail2Ban = new Fail2BanOptions
    {
        Enabled = true,
        MaxRetry = 3,
        BanTime = TimeSpan.FromHours(10),
        FindTime = TimeSpan.FromMinutes(10)
    },
    VerifyDns = true,
    CheckPort = true
};

var result = await deployer.DeployAsync(deployOptions, progress => Console.WriteLine(progress));

if (result.Success)
{
    Console.WriteLine("Деплой успешно завершен!");
    Console.WriteLine(result.Logs);
}
else
{
    Console.WriteLine($"Ошибка деплоя: {result.Message}");
}
```

### 2. Добавление и удаление пользователей на уже работающем сервере

```csharp
var manager = provider.GetRequiredService<IHysteriaManager>();

var connection = new ServerConnectionInfo
{
    Host = "192.168.1.100",
    Username = "root",
    Password = "MySecretServerPassword"
};

// Получить список пользователей
var users = await manager.GetUsersAsync(connection);

// Добавить нового пользователя (автоматически переводит аутентификацию в режим userpass и перезапускает контейнер)
await manager.AddOrUpdateUserAsync(connection, username: "alex", password: "alex-password");

// Удалить пользователя
await manager.RemoveUserAsync(connection, username: "old_user");

// Получить актуальные логи
var logs = await manager.GetLogsAsync(connection, lines: 50);

// Получить статус сервиса
var status = await manager.GetStatusAsync(connection);
Console.WriteLine($"Running: {status.ContainerRunning}");
```

## Запуск тестов

```bash
dotnet test
```
