namespace DeployHysteria.Core.Models.Hysteria;

public record HysteriaUser(string Username, string Password);

public record AcmeConfig
{
    public List<string> Domains { get; set; } = [];
    public string Email { get; set; } = string.Empty;
    public string Dir { get; set; } = "/acme";
}

public record AclConfig
{
    public List<string> Inline { get; set; } =
    [
        "reject(geoip:ru)",
        "reject(geosite:category-ru)",
        "direct(all)"
    ];
}

public record ObfsSalamanderConfig
{
    public string Password { get; set; } = string.Empty;
}

public record ObfsConfig
{
    public string Type { get; set; } = "salamander";
    public ObfsSalamanderConfig? Salamander { get; set; }
}

public record MasqueradeFileConfig
{
    public string Dir { get; set; } = "/var/www/masq";
}

public record MasqueradeProxyConfig
{
    public string Url { get; set; } = string.Empty;
    public bool RewriteHost { get; set; } = true;
}

public record MasqueradeConfig
{
    public string Type { get; set; } = "file";
    public MasqueradeFileConfig? File { get; set; }
    public MasqueradeProxyConfig? Proxy { get; set; }
    public string? String { get; set; }

    public static MasqueradeConfig CreateFile(string dir = "/var/www/masq") => new()
    {
        Type = "file",
        File = new MasqueradeFileConfig { Dir = dir }
    };

    public static MasqueradeConfig CreateProxy(string url, bool rewriteHost = true) => new()
    {
        Type = "proxy",
        Proxy = new MasqueradeProxyConfig { Url = url, RewriteHost = rewriteHost }
    };
}

public record BandwidthConfig
{
    public string? Up { get; set; }
    public string? Down { get; set; }

    public bool HasLimit => !string.IsNullOrWhiteSpace(Up) || !string.IsNullOrWhiteSpace(Down);
}

public record AuthConfig
{
    public string Type { get; set; } = "password";
    public string? Password { get; set; }
    public Dictionary<string, string>? Userpass { get; set; }

    public static AuthConfig FromSinglePassword(string password) => new()
    {
        Type = "password",
        Password = password
    };

    public static AuthConfig FromUsers(IEnumerable<HysteriaUser> users)
    {
        var dict = users.ToDictionary(u => u.Username, u => u.Password);
        return new AuthConfig
        {
            Type = "userpass",
            Userpass = dict
        };
    }
}

public record HysteriaConfig
{
    public string Listen { get; set; } = ":443";
    public AcmeConfig Acme { get; set; } = new();
    public AclConfig? Acl { get; set; }
    public ObfsConfig? Obfs { get; set; }
    public AuthConfig Auth { get; set; } = new();
    public MasqueradeConfig? Masquerade { get; set; }
    public BandwidthConfig? Bandwidth { get; set; }

    public IReadOnlyList<HysteriaUser> GetUsers()
    {
        if (Auth.Type.Equals("userpass", StringComparison.OrdinalIgnoreCase) && Auth.Userpass != null)
        {
            return Auth.Userpass.Select(kv => new HysteriaUser(kv.Key, kv.Value)).ToList();
        }

        if (!string.IsNullOrEmpty(Auth.Password))
        {
            return [new HysteriaUser("default", Auth.Password)];
        }

        return [];
    }

    public void AddOrUpdateUser(string username, string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        if (!Auth.Type.Equals("userpass", StringComparison.OrdinalIgnoreCase))
        {
            var existingUsers = GetUsers();
            var dict = existingUsers.ToDictionary(u => u.Username, u => u.Password);
            dict[username] = password;
            Auth = new AuthConfig
            {
                Type = "userpass",
                Userpass = dict
            };
        }
        else
        {
            Auth.Userpass ??= new Dictionary<string, string>();
            Auth.Userpass[username] = password;
        }
    }

    public bool RemoveUser(string username)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);

        if (Auth.Type.Equals("userpass", StringComparison.OrdinalIgnoreCase) && Auth.Userpass != null)
        {
            return Auth.Userpass.Remove(username);
        }

        if (Auth.Type.Equals("password", StringComparison.OrdinalIgnoreCase) && username.Equals("default", StringComparison.OrdinalIgnoreCase))
        {
            Auth.Password = null;
            return true;
        }

        return false;
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Listen))
        {
            throw new InvalidOperationException("Listen port/address cannot be empty.");
        }

        if (Acme.Domains == null || Acme.Domains.Count == 0 || Acme.Domains.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException("ACME domains list must contain at least one valid domain.");
        }

        if (string.IsNullOrWhiteSpace(Acme.Email))
        {
            throw new InvalidOperationException("ACME email cannot be empty.");
        }

        if (Auth == null)
        {
            throw new InvalidOperationException("Auth configuration is required.");
        }

        if (Auth.Type.Equals("password", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(Auth.Password))
        {
            throw new InvalidOperationException("Password must be specified for password auth type.");
        }

        if (Auth.Type.Equals("userpass", StringComparison.OrdinalIgnoreCase) &&
            (Auth.Userpass == null || Auth.Userpass.Count == 0))
        {
            throw new InvalidOperationException("At least one user must be specified for userpass auth type.");
        }

        if (Obfs != null && Obfs.Type.Equals("salamander", StringComparison.OrdinalIgnoreCase))
        {
            if (Obfs.Salamander == null || string.IsNullOrWhiteSpace(Obfs.Salamander.Password))
            {
                throw new InvalidOperationException("Salamander obfs password cannot be empty.");
            }
        }
    }
}
