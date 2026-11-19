using System;
using System.Text;

namespace OpenGitBlame.Core;

/// <summary>
/// Turns a <see cref="BlameInfo"/> into the text shown in the editor.
/// </summary>
public static class BlameFormatter
{
    public const string DefaultFormat = "{author}, {ago} • {summary}";

    public const string UncommittedText = "You, uncommitted changes";

    /// <summary>
    /// Replaces the placeholders in <paramref name="format"/>: <c>{author}</c>, <c>{email}</c>, <c>{date}</c>,
    /// <c>{ago}</c>, <c>{summary}</c> and <c>{hash}</c>. Unknown placeholders are left as they are.
    /// </summary>
    public static string Format(string format, BlameInfo info, DateTimeOffset now)
    {
        if (!info.IsCommitted)
            return UncommittedText;

        var result = new StringBuilder(format)
            .Replace("{author}", info.Author)
            .Replace("{email}", info.AuthorEmail)
            .Replace("{date}", info.AuthorTime.ToString("yyyy-MM-dd"))
            .Replace("{ago}", TimeAgo(info.AuthorTime, now))
            .Replace("{summary}", info.Summary)
            .Replace("{hash}", info.ShortHash);

        return result.ToString();
    }

    public static string TimeAgo(DateTimeOffset time, DateTimeOffset now)
    {
        var elapsed = now - time;

        if (elapsed.TotalMinutes < 1) return "just now";
        if (elapsed.TotalHours < 1) return Plural((int)elapsed.TotalMinutes, "minute");
        if (elapsed.TotalDays < 1) return Plural((int)elapsed.TotalHours, "hour");
        if (elapsed.TotalDays < 7) return Plural((int)elapsed.TotalDays, "day");
        if (elapsed.TotalDays < 30) return Plural((int)(elapsed.TotalDays / 7), "week");
        if (elapsed.TotalDays < 365) return Plural((int)(elapsed.TotalDays / 30), "month");
        return Plural((int)(elapsed.TotalDays / 365), "year");
    }

    private static string Plural(int count, string unit) =>
        count == 1 ? $"1 {unit} ago" : $"{count} {unit}s ago";
}
