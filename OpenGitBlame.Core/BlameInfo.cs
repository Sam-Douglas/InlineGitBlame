using System;

namespace OpenGitBlame.Core;

/// <summary>
/// The commit that last changed a line.
/// </summary>
public sealed class BlameInfo(string commitHash, string author, string authorEmail, DateTimeOffset authorTime, string summary)
{
    private const string UncommittedHash = "0000000000000000000000000000000000000000";

    public string CommitHash { get; } = commitHash;

    public string ShortHash => CommitHash.Length > 7 ? CommitHash.Substring(0, 7) : CommitHash;

    public string Author { get; } = author;

    public string AuthorEmail { get; } = authorEmail;

    public DateTimeOffset AuthorTime { get; } = authorTime;

    public string Summary { get; } = summary;

    /// <summary>False for lines that only exist in the working copy or the editor.</summary>
    public bool IsCommitted => CommitHash != UncommittedHash;
}
