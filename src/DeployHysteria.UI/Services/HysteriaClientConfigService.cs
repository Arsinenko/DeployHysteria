using DeployHysteria.Core.Deployment;
using DeployHysteria.Core.Models.Hysteria;
using QRCoder;
using System.Text;

namespace DeployHysteria.UI.Services;

public class HysteriaClientConfigService : IHysteriaClientConfigService
{
    public string GenerateUri(ClientConnectionInfo info)
    {
        ArgumentNullException.ThrowIfNull(info);
        ArgumentException.ThrowIfNullOrWhiteSpace(info.ServerHost);
        ArgumentException.ThrowIfNullOrWhiteSpace(info.Password);

        // Format userinfo: user:pass or pass
        string userinfo;
        if (!string.IsNullOrWhiteSpace(info.Username) && !info.Username.Equals("default", StringComparison.OrdinalIgnoreCase))
        {
            userinfo = $"{Uri.EscapeDataString(info.Username)}:{Uri.EscapeDataString(info.Password)}";
        }
        else
        {
            userinfo = Uri.EscapeDataString(info.Password);
        }

        var sb = new StringBuilder();
        sb.Append($"hysteria2://{userinfo}@{info.ServerHost}:{info.Port}/?");

        var queryParams = new List<string>();

        if (info.Insecure)
        {
            queryParams.Add("insecure=1");
        }

        var sni = !string.IsNullOrWhiteSpace(info.Sni) ? info.Sni : info.ServerHost;
        queryParams.Add($"sni={Uri.EscapeDataString(sni)}");

        if (!string.IsNullOrWhiteSpace(info.ObfsType) && !string.IsNullOrWhiteSpace(info.ObfsPassword))
        {
            queryParams.Add($"obfs={Uri.EscapeDataString(info.ObfsType)}");
            queryParams.Add($"obfs-password={Uri.EscapeDataString(info.ObfsPassword)}");
        }

        sb.Append(string.Join("&", queryParams));

        var tag = !string.IsNullOrWhiteSpace(info.Tag)
            ? info.Tag
            : (!string.IsNullOrWhiteSpace(info.Username) ? $"{info.Username}@{info.ServerHost}" : info.ServerHost);

        sb.Append($"#{Uri.EscapeDataString(tag)}");

        return sb.ToString();
    }

    public string GenerateTextConfig(ClientConnectionInfo info)
    {
        ArgumentNullException.ThrowIfNull(info);

        var uri = GenerateUri(info);
        var sb = new StringBuilder();
        sb.AppendLine("==================================================");
        sb.AppendLine("        HYSTERIA 2 CONNECTION PROFILE            ");
        sb.AppendLine("==================================================");
        sb.AppendLine($"Server (Host):        {info.ServerHost}");
        sb.AppendLine($"Port:                 {info.Port}");
        sb.AppendLine($"Protocol:             Hysteria 2 (UDP/QUIC)");
        sb.AppendLine($"SNI / ServerName:     {(!string.IsNullOrWhiteSpace(info.Sni) ? info.Sni : info.ServerHost)}");

        if (!string.IsNullOrWhiteSpace(info.Username) && !info.Username.Equals("default", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine($"Auth Mode:            User & Password");
            sb.AppendLine($"Username:             {info.Username}");
            sb.AppendLine($"Password:             {info.Password}");
        }
        else
        {
            sb.AppendLine($"Auth Mode:            Single Password");
            sb.AppendLine($"Password:             {info.Password}");
        }

        if (!string.IsNullOrWhiteSpace(info.ObfsType) && !string.IsNullOrWhiteSpace(info.ObfsPassword))
        {
            sb.AppendLine($"Obfuscation:          {info.ObfsType}");
            sb.AppendLine($"Obfuscation Password: {info.ObfsPassword}");
        }
        else
        {
            sb.AppendLine("Obfuscation:          None");
        }

        sb.AppendLine($"Insecure (Skip Cert): {info.Insecure}");
        sb.AppendLine();
        sb.AppendLine("==================================================");
        sb.AppendLine("              IMPORT URI (ONE-CLICK)              ");
        sb.AppendLine("==================================================");
        sb.AppendLine(uri);
        sb.AppendLine();
        sb.AppendLine("Paste this URI directly into v2rayN, Nekoray, Matsuri,");
        sb.AppendLine("NekoBox, Sing-box, Clash-verge, or any Hysteria 2 client.");

        return sb.ToString();
    }

    public byte[] GenerateQrCodePng(string content, int pixelsPerModule = 15)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.M);
        var qrCode = new PngByteQRCode(qrCodeData);
        return qrCode.GetGraphic(pixelsPerModule);
    }

    public ClientConnectionInfo CreateFromConfig(HysteriaConfig config, string username, string password, string? host = null)
    {
        ArgumentNullException.ThrowIfNull(config);

        var domain = config.Acme.Domains.FirstOrDefault() ?? host ?? "127.0.0.1";
        var port = HysteriaDeployer.ExtractPortFromListen(config.Listen);

        string? obfsType = null;
        string? obfsPassword = null;

        if (config.Obfs != null && !string.IsNullOrWhiteSpace(config.Obfs.Salamander?.Password))
        {
            obfsType = config.Obfs.Type;
            obfsPassword = config.Obfs.Salamander.Password;
        }

        return new ClientConnectionInfo
        {
            ServerHost = domain,
            Port = port,
            Username = username,
            Password = password,
            Sni = domain,
            ObfsType = obfsType,
            ObfsPassword = obfsPassword,
            Insecure = false,
            Tag = $"{username}@{domain}"
        };
    }
}
