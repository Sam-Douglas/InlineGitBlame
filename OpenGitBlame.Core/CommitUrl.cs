using System;
using System.Text.RegularExpressions;

namespace OpenGitBlame.Core;

/// <summary>
/// Works out the web page for a commit from the repository's remote URL.
/// </summary>
public static class CommitUrl
{
    // git@host:owner/repo.git
    private static readonly Regex ScpStyle = new(@"^(?:[^@/]+@)?(?<host>[^:/\\]{2,}):(?<path>.+)$");

    // git@ssh.dev.azure.com:v3/org/project/repo
    private static readonly Regex AzureSsh = new(@"^v3/(?<org>[^/]+)/(?<project>[^/]+)/(?<repo>[^/]+)$");

    /// <summary>Returns null when the remote isn't something a browser can open.</summary>
    public static string? From(string remoteUrl, string commitHash)
    {
        string? repoUrl = ToWebUrl(remoteUrl.Trim());
        if (repoUrl == null) return null;

        var uri = new Uri(repoUrl);

        if (uri.Host.Contains("bitbucket"))
            return $"{repoUrl}/commits/{commitHash}";

        if (uri.Host.Contains("gitlab"))
            return $"{repoUrl}/-/commit/{commitHash}";

        return $"{repoUrl}/commit/{commitHash}";
    }

    /// <summary>Converts a clone URL to the https address of the repository's home page.</summary>
    internal static string? ToWebUrl(string remoteUrl)
    {
        string host;
        string path;

        if (Uri.TryCreate(remoteUrl, UriKind.Absolute, out var uri))
        {
            if (uri.IsFile) return null;

            host = uri.Host;
            path = uri.AbsolutePath;
        }
        else if (ScpStyle.Match(remoteUrl) is { Success: true } match)
        {
            host = match.Groups["host"].Value;
            path = match.Groups["path"].Value;
        }
        else
        {
            return null;
        }

        path = path.Trim('/');
        if (path.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
            path = path.Substring(0, path.Length - 4);

        if (host == "ssh.dev.azure.com" && AzureSsh.Match(path) is { Success: true } azure)
            return $"https://dev.azure.com/{azure.Groups["org"].Value}/{azure.Groups["project"].Value}/_git/{azure.Groups["repo"].Value}";

        if (host.StartsWith("ssh.", StringComparison.OrdinalIgnoreCase))
            host = host.Substring(4);

        return path.Length == 0 ? null : $"https://{host}/{path}";
    }
}
