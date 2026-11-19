using System;
using System.Collections.Generic;
using System.IO;

namespace OpenGitBlame.Core;

/// <summary>
/// Reads the output of <c>git blame --porcelain</c>.
/// </summary>
public static class BlameParser
{
    /// <summary>Returns the blame for each line of the file, indexed from zero.</summary>
    public static IReadOnlyList<BlameInfo> Parse(string porcelain)
    {
        var commits = new Dictionary<string, CommitBuilder>();
        var lines = new List<BlameInfo?>();

        CommitBuilder? current = null;
        int currentLine = 0;

        using var reader = new StringReader(porcelain);
        string? text;
        while ((text = reader.ReadLine()) != null)
        {
            // Each blamed line ends with its content, prefixed by a tab.
            if (text.StartsWith("\t", StringComparison.Ordinal))
            {
                if (current != null)
                    SetLine(lines, currentLine - 1, current.Build());
                continue;
            }

            if (TryParseHeader(text, out string hash, out int finalLine))
            {
                if (!commits.TryGetValue(hash, out current))
                    commits[hash] = current = new CommitBuilder(hash);
                currentLine = finalLine;
                continue;
            }

            if (current == null) continue;

            int space = text.IndexOf(' ');
            string key = space < 0 ? text : text.Substring(0, space);
            string value = space < 0 ? "" : text.Substring(space + 1);

            switch (key)
            {
                case "author": current.Author = value; break;
                case "author-mail": current.AuthorEmail = value.Trim('<', '>'); break;
                case "author-time" when long.TryParse(value, out long seconds): current.AuthorTime = seconds; break;
                case "author-tz": current.AuthorZone = value; break;
                case "summary": current.Summary = value; break;
            }
        }

        var result = new BlameInfo[lines.Count];
        for (int i = 0; i < lines.Count; i++)
            result[i] = lines[i] ?? Missing;
        return result;
    }

    private static readonly BlameInfo Missing = new("", "", "", DateTimeOffset.MinValue, "");

    private static bool TryParseHeader(string text, out string hash, out int finalLine)
    {
        hash = "";
        finalLine = 0;

        // "<40 hex hash> <original line> <final line> [<lines in group>]"
        var parts = text.Split(' ');
        if (parts.Length < 3 || parts[0].Length != 40 || !IsHex(parts[0]))
            return false;

        if (!int.TryParse(parts[2], out finalLine) || finalLine < 1)
            return false;

        hash = parts[0];
        return true;
    }

    private static bool IsHex(string value)
    {
        foreach (char c in value)
        {
            if (!Uri.IsHexDigit(c)) return false;
        }
        return true;
    }

    private static void SetLine(List<BlameInfo?> lines, int index, BlameInfo info)
    {
        while (lines.Count <= index)
            lines.Add(null);
        lines[index] = info;
    }

    private sealed class CommitBuilder(string hash)
    {
        private BlameInfo? _built;

        public string Author { get; set; } = "";
        public string AuthorEmail { get; set; } = "";
        public long AuthorTime { get; set; }
        public string AuthorZone { get; set; } = "+0000";
        public string Summary { get; set; } = "";

        public BlameInfo Build() => _built ??= new BlameInfo(hash, Author, AuthorEmail, ToLocalTime(AuthorTime, AuthorZone), Summary);

        private static DateTimeOffset ToLocalTime(long seconds, string zone)
        {
            var utc = DateTimeOffset.FromUnixTimeSeconds(seconds);

            // "+0130" -> 1h30m ahead of UTC.
            if (zone.Length == 5
                && int.TryParse(zone.Substring(1, 2), out int hours)
                && int.TryParse(zone.Substring(3, 2), out int minutes))
            {
                var offset = new TimeSpan(hours, minutes, 0);
                return utc.ToOffset(zone[0] == '-' ? offset.Negate() : offset);
            }

            return utc;
        }
    }
}
