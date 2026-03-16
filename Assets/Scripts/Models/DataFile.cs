using System;
using System.IO;

public class DataFile
{
    public string id;
    public string name;
    public string description;
    public string type;
    public string link;

    public DataFile() { }

    public DataFile(string id, string name, string description, string type, string link)
    {
        this.id = id;
        this.name = name;
        this.description = description;
        this.type = type;
        this.link = link;
    }

    public static string ResolveLink(string rawLink, DBConfig config)
    {
        if (string.IsNullOrWhiteSpace(rawLink))
            return rawLink;

        string trimmed = rawLink.Trim();
        if (IsAbsoluteUri(trimmed) || IsUncPath(trimmed))
            return trimmed;

        if (Path.IsPathRooted(trimmed))
            return NormalizePath(trimmed);

        bool preferWeb = config != null && config.preferWebFileLinks && !string.IsNullOrWhiteSpace(config.webFileRoot);
        if (preferWeb)
            return CombineUrl(config.webFileRoot, trimmed);

        string baseLocal = GetBasePath(config);
        if (string.IsNullOrWhiteSpace(baseLocal))
            return NormalizePath(trimmed);

        string relative = trimmed.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        return NormalizePath(Path.Combine(baseLocal, relative));
    }

    private static string GetBasePath(DBConfig config)
    {
        if (config == null)
            return null;

        if (config.useServerShareForRelativeLinks
            && !string.IsNullOrWhiteSpace(config.serverIp)
            && !string.IsNullOrWhiteSpace(config.serverFileShareName))
        {
            string host = config.serverIp.Trim().TrimStart('\\').TrimEnd('\\');
            string share = config.serverFileShareName.Trim().Trim('\\', '/');
            if (!string.IsNullOrWhiteSpace(host) && !string.IsNullOrWhiteSpace(share))
                return "\\\\" + host + "\\" + share;
        }

        return config.localFileRoot;
    }

    private static bool IsAbsoluteUri(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
            return false;

        string scheme = uri.Scheme.ToLowerInvariant();
        return scheme == "http" || scheme == "https" || scheme == "file" || scheme == "ftp";
    }

    private static bool IsUncPath(string value)
    {
        return value.StartsWith("\\\\", StringComparison.Ordinal);
    }

    private static string NormalizePath(string value)
    {
        try
        {
            return Path.GetFullPath(value);
        }
        catch
        {
            return value;
        }
    }

    private static string CombineUrl(string baseUrl, string relative)
    {
        string left = baseUrl.TrimEnd('/', '\\');
        string right = relative.TrimStart('/', '\\');
        return left + "/" + right.Replace('\\', '/');
    }
}
