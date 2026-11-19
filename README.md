# DeployHysteria

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![C# 13/14](https://img.shields.io/badge/C%23-13%2F14-239120?logo=csharp)](https://docs.microsoft.com/dotnet/csharp/)
[![Avalonia UI 12](https://img.shields.io/badge/Avalonia-12.0-8A2BE2)](https://avaloniaui.net/)
[![Hysteria 2](https://img.shields.io/badge/Hysteria-v2-FF4500)](https://v2.hysteria.network/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

[🇷🇺 Русский](#deployhysteria-русский) | [🇬🇧 English](#deployhysteria-english)

---

# DeployHysteria (Русский)

**DeployHysteria** — это кроссплатформенное приложение и библиотека на **.NET 10**, предназначенные для автоматического, безопасного и надежного развертывания сервера **Hysteria 2** на чистых Linux-серверах (Ubuntu / Debian) через SSH всего в один клик.

Решение включает в себя:
- **DeployHysteria.Desktop**: кроссплатформенное графическое приложение (Linux, Windows, macOS) на Avalonia UI 12 с поддержкой темной/светлой темы и русской/английской локализации.
- **DeployHysteria.Android**: мобильный клиент на Avalonia UI для управления и деплоя прямо со смартфона.
- **DeployHysteria.Core**: библиотека классов .NET 10 для встраивания процессов деплоя, провижининга и управления сервером в ваши собственные CLI-утилиты, боты или веб-сервисы.

---

## 📑 Содержание (RU)

1. [Возможности](#возможности-ru)
2. [Простая инструкция (Быстрый старт)](#простая-инструкция-быстрый-старт-ru)
3. [Настройка безопасности: UFW и Fail2Ban](#настройка-безопасности-ufw-и-fail2ban-ru)
4. [Параметры ACL: Блокировка российских серверов по умолчанию](#параметры-acl-блокировка-российских-серверов-по-умолчанию-ru)
5. [Расширенные параметры (Advanced Mode)](#расширенные-параметры-advanced-mode-ru)
6. [Управление работающим сервером](#управление-работающим-сервером-ru)
7. [Примеры кода (.NET Core API)](#примеры-кода-net-core-api-ru)
8. [Сборка и запуск тестов](#сборка-и-запуск-тестов-ru)

---

<a id="возможности-ru"></a>
## 🚀 Возможности

- **Полная автоматизация подготовки сервера (Provisioning)**:
  - Обновление системных пакетов `apt-get`.
  - Установка официального Docker и Docker Compose.
  - Автоматическая настройка фаервола **UFW** (открытие только необходимых портов).
  - Установка и активация службы **Fail2Ban** для защиты SSH от перебора паролей.
- **Предварительные проверки (Pre-flight checks)**:
  - Валидация DNS: проверка, что указанный ACME-домен действительно ссылается на IP сервера.
  - Проверка занятости порта 443 (или пользовательского порта) удаленным процессом до начала деплоя.
- **Протокол Hysteria 2 "под ключ"**:
  - Автоматический выпуск TLS-сертификатов Let's Encrypt через ACME.
  - Маскировка трафика (Masquerade) — локальный сайт или проксирование на внешний ресурс.
  - Обфускация протокола (Salamander Obfs) для обхода DPI.
  - Контроль полосы пропускания (Upload/Download bandwidth).
  - Списки контроля доступа (ACL) с защитой от закольцовывания и фильтрацией трафика.
- **Интеграция с клиентами**:
  - Генерация ссылок `hysteria2://...` для v2rayN, Sing-box, NekoBox, Clash Verge, Matsuri.
  - Отображение и экспорт QR-кода в файл `.png`.
  - Экспорт параметров подключения в удобный текстовый файл `.txt`.
- **Управление пользователями на лету**:
  - Добавление и удаление пользователей без прямого редактирования файлов.
  - Просмотр логов контейнера и статуса служб (UFW, Fail2Ban, Docker).

---

<a id="простая-инструкция-быстрый-старт-ru"></a>
## ⚡ Простая инструкция (Быстрый старт)

### Требования к серверу:
1. Чистый VPS / VDS с операционной системой **Ubuntu (20.04+)** или **Debian (11+)**.
2. Пользователь `root` (или пользователь с полными правами sudo).
3. Доменное имя, у которого **A-запись** указывает на публичный IP-адрес вашего сервера (необходимо для получения бесплатного SSL-сертификата Let's Encrypt).

### Шаги для развертывания через GUI:

1. **Запустите приложение**:
   ```bash
   dotnet run --project src/DeployHysteria.Desktop
   ```
2. **Заполните основные поля** (в простом режиме):
   - **Host (IP)**: IP-адрес вашего сервера.
   - **Пароль SSH**: пароль пользователя `root` (или выберите авторизацию по SSH-ключу).
   - **Домен**: ваш домен (например, `vpn.mydomain.com`).
   - **Email**: email для регистрации сертификата Let's Encrypt.
   - Нажмите кнопку **"Сгенерировать пароли"** (пароль клиента и обфускации заполнятся автоматически).
3. **Нажмите "Развернуть"**:
   - Приложение выполнит DNS-проверку, подключится по SSH, установит Docker, настроит UFW и Fail2Ban, сгенерирует конфигурацию и запустит контейнер.
4. **Подключитесь к VPN**:
   - После успешного деплоя откроется диалог с **QR-кодом** и кнопками копирования ссылки `hysteria2://...` или сохранения конфигурации. Отсканируйте код в приложении Hysteria 2 на смартфоне или вставьте ссылку в v2rayN / Sing-box на ПК.

---

<a id="настройка-безопасности-ufw-и-fail2ban-ru"></a>
## 🛡️ Настройка безопасности: UFW и Fail2Ban

DeployHysteria по умолчанию обеспечивает базовую защиту целевого сервера Linux от сетевого сканирования и брутфорс-атак.

### 1. Фаервол UFW (Uncomplicated Firewall)
При включенной опции фаервола (`Firewall.Enabled = true`, включено по умолчанию):
- На сервере устанавливается пакет `ufw`.
- Открывается порт SSH (по умолчанию `22/tcp` или пользовательский порт).
- Открывается порт Hysteria 2 (по умолчанию `443/tcp` и `443/udp`). Hysteria 2 работает по протоколу QUIC поверх UDP, а TCP требуется для прохождения ACME HTTP-01 челленджа Let's Encrypt.
- Фаервол переводится в активный режим командой `ufw --force enable`.
- Все остальные входящие порты **блокируются по умолчанию**.

### 2. Защита от подбора паролей Fail2Ban
При включенной опции (`Fail2Ban.Enabled = true`, включено по умолчанию):
- На сервере устанавливается пакет `fail2ban`.
- В директорию `/etc/fail2ban/jail.d/hysteria-ssh.local` загружается конфигурация:
  ```ini
  [DEFAULT]
  bantime = 36000
  findtime = 600
  maxretry = 3

  [sshd]
  enabled = true
  port = 22
  maxretry = 3
  bantime = 36000
  findtime = 600
  ```
- **Параметры по умолчанию**:
  - `maxretry = 3`: блокировка наступает уже после 3 неудачных попыток ввода пароля.
  - `findtime = 10m` (600 секунд): окно подсчета неудачных попыток.
  - `bantime = 10h` (36000 секунд): длительность бана IP-адреса нарушителя (10 часов).
- Служба активируется в автозагрузке и перезапускается (`systemctl enable --now fail2ban && systemctl restart fail2ban`).

---

<a id="параметры-acl-блокировка-российских-серверов-по-умолчанию-ru"></a>
## 🚫 Параметры ACL: Блокировка российских серверов по умолчанию

В DeployHysteria по умолчанию настроены правила **ACL (Access Control List)**, которые **блокируют маршрутизацию трафика на российские ресурсы**:

```yaml
acl:
  inline:
    - "reject(geoip:ru)"
    - "reject(geosite:category-ru)"
    - "direct(all)"
```

### Зачем это нужно?
1. **Предотвращение закольцовывания и нерациональной маршрутизации**: если пользователь находится в РФ и использует зарубежный сервер Hysteria 2, отправка запросов к российским сервисам через зарубежный сервер увеличивает задержку (пинг), тратит трафик сервера и создает бессмысленную нагрузку.
2. **Блокировки со стороны российских сервисов**: многие банки, государственные порталы (Госуслуги, ФНС) и медиасервисы блокируют доступ с иностранных IP-адресов датацентров или выдают постоянные капчи. Блокировка на сервере заставляет клиентское приложение (или браузер) обращаться к ним напрямую в обход туннеля.
3. **Безопасность**: изоляция сервера от обращений к ресурсам в зоне `.ru`.

### Значение директив:
- `reject(geoip:ru)`: отклоняет любые подключения к IP-адресам, зарегистрированным в GeoIP-базе за Российской Федерацией.
- `reject(geosite:category-ru)`: отклоняет DNS-запросы и соединения к доменам из категорий российских сайтов (.ru, .рф, сервисы Яндекса, VK и др.).
- `direct(all)`: весь остальной разрешенный трафик отправляется напрямую в интернет.

> **Примечание**: Если вам требуется разрешить доступ к российским ресурсам через ваш прокси-сервер, вы можете переопределить список `Acl.Inline` в конфигурации или передать пустой список, чтобы отключить фильтрацию.

---

<a id="расширенные-параметры-advanced-mode-ru"></a>
## ⚙️ Расширенные параметры (Advanced Mode)

В расширенном режиме (доступен по переключателю **"Расширенный режим"** в UI или через объект `DeployOptions` в коде) доступны следующие настройки:

| Параметр | По умолчанию | Описание |
| :--- | :--- | :--- |
| **SshPort** | `22` | Порт SSH на удаленном сервере |
| **Username** | `root` | Пользователь для входа по SSH |
| **Auth Type** | `Password` | Авторизация по паролю или приватному SSH-ключу (с поддержкой passphrase) |
| **Listen Port** | `443` | Порт, на котором слушает демон Hysteria 2 (UDP/TCP) |
| **RemoteDirectory** | `/opt/hysteria2` | Рабочая директория на сервере для `docker-compose.yml` и `config.yaml` |
| **VerifyDns** | `true` | Проверять соответствие A-записи домена IP-адресу сервера перед началом деплоя |
| **CheckPort** | `true` | Проверять, не занят ли порт 443 другим веб-сервером (nginx/apache/caddy) |
| **UpdatePackages** | `true` | Выполнять ли `apt-get update && apt-get upgrade -y` при провижининге |
| **InstallDocker** | `true` | Проверять и устанавливать Docker + Docker Compose, если они отсутствуют |
| **Firewall.Enabled** | `true` | Установка и включение UFW |
| **Fail2Ban.Enabled** | `true` | Установка и включение Fail2Ban |
| **Fail2Ban.MaxRetry** | `3` | Количество попыток перед блокировкой |
| **Fail2Ban.BanTime** | `10 hours` | Время бана IP-адреса |
| **Obfs Type** | `salamander` | Протокол обфускации (Salamander) |
| **Obfs Password** | *(случайный)* | Ключ обфускации трафика |
| **Masquerade Type** | `file` | Тип маскировки: локальные файлы (`file`) или обратный прокси (`proxy`) |
| **Masquerade Dir** | `/var/www/masq` | Директория для файлов сайта-заглушки при типе `file` |
| **Masquerade URL** | `https://bing.com` | URL назначения при маскировке типа `proxy` (с `rewriteHost: true`) |
| **Bandwidth Up/Down** | *(без лимита)* | Ограничение скорости сервера (например, `100 mbps`, `1 gbps`) |

---

<a id="управление-работающим-сервером-ru"></a>
## 👥 Управление работающим сервером

Вкладка **"Управление" (Manage)** в приложении позволяет подключиться к уже настроенному серверу:
- **Пользователи**: просмотр списка существующих учетных записей, создание новых пользователей с персональными паролями, удаление пользователей. При добавлении второго пользователя конфигурация автоматически переводится в режим `userpass`.
- **QR-коды**: генерация индивидуального QR-кода и ссылки подключения для каждого созданного пользователя.
- **Статус сервиса**: проверка активности Docker-контейнера, состояния UFW и Fail2Ban.
- **Логи**: чтение и обновление логов Hysteria 2 в реальном времени прямо из контейнера.
- **Перезапуск**: перезапуск контейнера Hysteria 2 в один клик.

---

<a id="примеры-кода-net-core-api-ru"></a>
## 💻 Примеры кода (.NET Core API)

### 1. Деплой с расширенными параметрами

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

var options = new DeployOptions
{
    ServerConnection = new ServerConnectionInfo
    {
        Host = "203.0.113.10",
        Port = 22,
        Username = "root",
        Password = "SuperStrongRootPassword!"
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
        // Блокировка российских сетей и сайтов по умолчанию
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
            Salamander = new ObfsSalamanderConfig { Password = "SecretSalamanderPassword123" }
        },
        Auth = AuthConfig.FromSinglePassword("ClientPassword123"),
        Masquerade = MasqueradeConfig.CreateProxy("https://bing.com", rewriteHost: true),
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

var result = await deployer.DeployAsync(options, progress => Console.WriteLine($"[DEPLOY] {progress}"));

if (result.Success)
{
    Console.WriteLine("Деплой успешно завершен!");
}
else
{
    Console.WriteLine($"Ошибка: {result.Message}");
}
```

---

<a id="сборка-и-запуск-тестов-ru"></a>
## 🔨 Сборка и запуск тестов

### Сборка решения:
```bash
dotnet build
```

### Запуск тестов:
Проект покрыт более чем 75 модульными тестами (Core + UI):
```bash
dotnet test
```

---
---

# DeployHysteria (English)

**DeployHysteria** is a cross-platform **.NET 10** desktop application and library for fully automated, secure, one-click deployment and lifecycle management of **Hysteria 2** servers on clean Linux hosts (Ubuntu / Debian) over SSH.

The solution includes:
- **DeployHysteria.Desktop**: Cross-platform desktop client (Linux, Windows, macOS) built on Avalonia UI 12 with theme switching and English/Russian localization.
- **DeployHysteria.Android**: Mobile client for deploying and managing nodes directly from an Android smartphone.
- **DeployHysteria.Core**: Reusable .NET 10 class library to embed deployment, provisioning, and server management into your own CLI tools, backend bots, or orchestration platforms.

---

## 📑 Table of Contents (EN)

1. [Features](#features-en)
2. [Quick Start Guide](#quick-start-guide-en)
3. [Security Setup: UFW & Fail2Ban](#security-setup-ufw--fail2ban-en)
4. [Default ACL Rules: Blocking Russian Traffic](#default-acl-rules-blocking-russian-traffic-en)
5. [Advanced Configuration Parameters](#advanced-configuration-parameters-en)
6. [Managing a Running Server](#managing-a-running-server-en)
7. [C# Code Examples (.NET API)](#c-code-examples-net-api-en)
8. [Building & Testing](#building--testing-en)

---

<a id="features-en"></a>
## 🚀 Features

- **Automated Server Provisioning**:
  - Operating system package updates (`apt-get update && apt-get upgrade -y`).
  - Automatic detection and installation of Docker and Docker Compose.
  - Automatic **UFW** firewall hardening (opens only necessary ports).
  - Automated **Fail2Ban** installation and configuration to safeguard SSH from brute-force attacks.
- **Pre-flight Checks**:
  - DNS resolution validation: ensures ACME domain resolves to the target server IP prior to provisioning.
  - Port conflict detection: verifies port 443 (or custom port) is free before starting services.
- **Full Hysteria 2 Protocol Support**:
  - Automated Let's Encrypt TLS certificate generation via ACME.
  - Protocol obfuscation via **Salamander** password to evade DPI blocks.
  - Traffic masquerading (file-based static site or transparent reverse proxy).
  - Upstream and downstream bandwidth rate limiting.
  - Out-of-the-box ACL rule sets with default geo-blocking.
- **Client Provisioning & QR Codes**:
  - Universal `hysteria2://...` link generation compatible with v2rayN, Sing-box, NekoBox, Clash Verge, and Matsuri.
  - In-app QR code display and export to `.png`.
  - Detailed `.txt` connection parameter export.
- **Active Server Management**:
  - Add or delete users on the fly without manual file edits.
  - Real-time container log viewer.
  - Service monitoring (Docker container, UFW firewall, Fail2Ban status).
  - Container restart capability.

---

<a id="quick-start-guide-en"></a>
## ⚡ Quick Start Guide

### Prerequisites:
1. Clean VPS running **Ubuntu (20.04+)** or **Debian (11+)**.
2. Root credentials or user with full sudo privileges.
3. Domain name with an **A record** resolving to your server's public IP address (required for Let's Encrypt SSL issuance).

### Deployment Steps (Desktop GUI):

1. **Launch the application**:
   ```bash
   dotnet run --project src/DeployHysteria.Desktop
   ```
2. **Fill in the basic parameters** (Simple Mode):
   - **Host (IP)**: Server public IP.
   - **SSH Password**: Root password (or toggle to SSH private key).
   - **Domain**: Your domain (e.g. `vpn.example.com`).
   - **Email**: Contact email for Let's Encrypt certificate issuance.
   - Click **"Generate Passwords"** to generate cryptographically secure passwords for client auth and obfuscation.
3. **Click "Deploy"**:
   - The deployer verifies DNS, connects over SSH, installs Docker, configures UFW and Fail2Ban, uploads configs, and starts the container.
4. **Connect your client**:
   - Once deployment completes, scan the generated **QR Code** or copy the `hysteria2://` URI into your client app (v2rayN, Sing-box, NekoBox, etc.).

---

<a id="security-setup-ufw--fail2ban-en"></a>
## 🛡️ Security Setup: UFW & Fail2Ban

DeployHysteria automatically configures production-grade security defaults to prevent server intrusion.

### 1. UFW Firewall
When enabled (`Firewall.Enabled = true`, default: `true`):
- Installs `ufw` non-interactively.
- Whitelists SSH port (default `22/tcp`).
- Whitelists Hysteria 2 port (default `443/tcp` and `443/udp`). QUIC traffic runs over UDP, while TCP is required for ACME HTTP-01 challenge verification.
- Enables the firewall automatically via `ufw --force enable`.
- Default policy blocks all other unsolicited incoming connections.

### 2. Fail2Ban SSH Brute-Force Protection
When enabled (`Fail2Ban.Enabled = true`, default: `true`):
- Installs `fail2ban`.
- Deploys a dedicated jail file to `/etc/fail2ban/jail.d/hysteria-ssh.local`:
  ```ini
  [DEFAULT]
  bantime = 36000
  findtime = 600
  maxretry = 3

  [sshd]
  enabled = true
  port = 22
  maxretry = 3
  bantime = 36000
  findtime = 600
  ```
- **Default Policy**:
  - `maxretry = 3`: Banned after 3 failed login attempts.
  - `findtime = 10m` (600 seconds): Evaluation window for failed attempts.
  - `bantime = 10h` (36000 seconds): Offenders are locked out for 10 hours.
- Enables and restarts the daemon: `systemctl enable --now fail2ban && systemctl restart fail2ban`.

---

<a id="default-acl-rules-blocking-russian-traffic-en"></a>
## 🚫 Default ACL Rules: Blocking Russian Traffic

DeployHysteria includes an **out-of-the-box ACL configuration** that blocks routing traffic to Russian IP addresses and domains:

```yaml
acl:
  inline:
    - "reject(geoip:ru)"
    - "reject(geosite:category-ru)"
    - "direct(all)"
```

### Why is this enabled by default?
1. **Routing Efficiency & Loop Prevention**: If the user connects from Russia to an overseas proxy, routing traffic back to Russian domestic servers creates redundant round-trips, high latency, and unnecessary server bandwidth consumption.
2. **Domestic Geo-restrictions & Captchas**: Many Russian public services, banking platforms, and local sites block foreign hosting provider IP ranges or present continuous Cloudflare/CAPTCHA challenges. Rejecting this traffic at the proxy server forces client applications to access domestic resources directly, avoiding connectivity issues.
3. **Security & Privacy**: Prevents unauthorized proxy relaying to `.ru` network zones.

### Directives Explained:
- `reject(geoip:ru)`: Immediately drops proxy connections aimed at any IP address belonging to the Russian Federation GeoIP database.
- `reject(geosite:category-ru)`: Drops connections and domain resolutions matching Russian TLDs (.ru, .рф) and popular domestic services in the GeoSite database.
- `direct(all)`: Directly routes all other international outbound traffic from the proxy server.

> **Customization**: To allow traffic to Russian services or define custom routing rules, override `Acl.Inline` in your `HysteriaConfig` or leave the list empty to disable ACL filtering.

---

<a id="advanced-configuration-parameters-en"></a>
## ⚙️ Advanced Configuration Parameters

When using **Advanced Mode** in the GUI or configuring `DeployOptions` via code, the following settings are available:

| Parameter | Default | Description |
| :--- | :--- | :--- |
| **SshPort** | `22` | Remote SSH port on the host |
| **Username** | `root` | SSH user |
| **Auth Type** | `Password` | SSH Password or Private Key authentication (with optional passphrase) |
| **Listen Port** | `443` | Port Hysteria 2 listens on (UDP/TCP) |
| **RemoteDirectory** | `/opt/hysteria2` | Remote folder where `docker-compose.yml` and `config.yaml` reside |
| **VerifyDns** | `true` | Validates that ACME domains resolve to the server IP before proceeding |
| **CheckPort** | `true` | Checks if port 443 is already bound by existing daemons (e.g. nginx/caddy) |
| **UpdatePackages** | `true` | Runs `apt-get update && apt-get upgrade -y` during provisioning |
| **InstallDocker** | `true` | Installs Docker Engine and Docker Compose plugin if missing |
| **Firewall.Enabled** | `true` | Activates UFW firewall with SSH & Hysteria rules |
| **Fail2Ban.Enabled** | `true` | Activates Fail2Ban jail for SSH |
| **Fail2Ban.MaxRetry**| `3` | Maximum login attempts before ban |
| **Fail2Ban.BanTime** | `10 hours` | Duration of the IP ban |
| **Obfs Type** | `salamander` | Obfuscation protocol (`salamander`) |
| **Obfs Password** | *(generated)* | Obfuscation shared secret |
| **Masquerade Type** | `file` | Masquerade mode: local directory (`file`) or reverse proxy (`proxy`) |
| **Masquerade Dir** | `/var/www/masq` | Local directory for mock website when using `file` mode |
| **Masquerade URL** | `https://bing.com` | Target URL when using `proxy` mode (`rewriteHost: true`) |
| **Bandwidth Up/Down**| *(unlimited)* | Optional bandwidth throttling (e.g., `100 mbps`, `1 gbps`) |

---

<a id="managing-a-running-server-en"></a>
## 👥 Managing a Running Server

The **Manage** tab allows interacting with an active server without terminal access:
- **User Accounts**: List active credentials, add new users with custom passwords, or revoke existing users. Switching from single user to multiple users automatically upgrades the config to `userpass` mode.
- **QR Codes**: View or export QR codes and `hysteria2://` URIs for any user.
- **Service Status**: Inspect real-time status of the Docker container, UFW firewall rules, and Fail2Ban active jails.
- **Live Logs**: Tail and refresh Hysteria 2 container logs.
- **Restart**: Gracefully restart the Hysteria 2 container with one click.

---

<a id="c-code-examples-net-api-en"></a>
## 💻 C# Code Examples (.NET API)

### Full Programmatic Deployment

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

var options = new DeployOptions
{
    ServerConnection = new ServerConnectionInfo
    {
        Host = "203.0.113.10",
        Port = 22,
        Username = "root",
        Password = "SuperStrongPassword123!"
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
        // Default: Block Russian IP ranges and domains
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
            Salamander = new ObfsSalamanderConfig { Password = "ObfsPasswordSecret!" }
        },
        Auth = AuthConfig.FromSinglePassword("ClientPassword123!"),
        Masquerade = MasqueradeConfig.CreateProxy("https://bing.com", rewriteHost: true),
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

var result = await deployer.DeployAsync(options, progress => Console.WriteLine($"[DEPLOY] {progress}"));

if (result.Success)
{
    Console.WriteLine("Deployment completed successfully!");
}
else
{
    Console.WriteLine($"Deployment error: {result.Message}");
}
```

---

<a id="building--testing-en"></a>
## 🔨 Building & Testing

### Build the Solution:
```bash
dotnet build
```

### Run Unit Tests:
The project is verified with over 75 unit tests across the Core library and UI ViewModels:
```bash
dotnet test
```

---

## 📄 License

This project is licensed under the [MIT License](LICENSE).
