namespace DeployHysteria.Core.Security;

public static class ShellSanitizer
{
    /// <summary>
    /// Escapes an argument for safe usage in POSIX shells.
    /// Wraps the argument in single quotes and escapes any embedded single quotes.
    /// </summary>
    public static string Escape(string? argument)
    {
        if (argument == null) return "''";
        if (argument.Length == 0) return "''";

        // In POSIX shell, 'arg' prevents all expansions except single quotes themselves.
        // A single quote inside single quotes can be safely represented as '\''.
        return $"'{argument.Replace("'", @"'\''")}'";
    }

    /// <summary>
    /// Validates that a filename/relative path contains only safe characters and no path traversal (..).
    /// </summary>
    public static bool IsSafePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        if (path.Contains("..")) return false;
        if (path.IndexOfAny(['\0', '\r', '\n']) >= 0) return false;
        return true;
    }
}
