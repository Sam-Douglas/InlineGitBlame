using OpenGitBlame.Core;
using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace OpenGitBlame.Git;

/// <summary>
/// Runs <c>git blame</c> for open documents and caches the result for each version of the text.
/// </summary>
[Export]
public sealed class BlameProvider
{
    private readonly ConditionalWeakTable<ITextBuffer, CachedBlame> _cache = new();

    /// <summary>
    /// Returns the blame for every line of <paramref name="snapshot"/>, or null if the file isn't tracked by git.
    /// Unsaved edits are passed to git, so lines that only exist in the editor show as uncommitted.
    /// </summary>
    public Task<IReadOnlyList<BlameInfo>?> GetBlameAsync(string filePath, ITextSnapshot snapshot)
    {
        var buffer = snapshot.TextBuffer;

        // The cached task runs on the thread pool and never needs the UI thread, so it's safe to share.
#pragma warning disable VSTHRD003
        if (_cache.TryGetValue(buffer, out var cached) && cached.Version == snapshot.Version.VersionNumber && cached.FilePath == filePath)
            return cached.Blame;
#pragma warning restore VSTHRD003

        var blame = Task.Run(() => RunBlameAsync(filePath, snapshot));

        _cache.Remove(buffer);
        _cache.Add(buffer, new CachedBlame(filePath, snapshot.Version.VersionNumber, blame));
        return blame;
    }

    /// <summary>Forgets the cached blame, e.g. after a commit may have been made outside Visual Studio.</summary>
    public void Invalidate(ITextBuffer buffer) => _cache.Remove(buffer);

    /// <summary>Returns the web page for a commit on the repository's default remote, if it has one.</summary>
    public async Task<string?> GetCommitUrlAsync(string filePath, string commitHash)
    {
        string? remote = await GitProcess.RunAsync(Path.GetDirectoryName(filePath), "ls-remote --get-url");
        return remote == null ? null : CommitUrl.From(remote, commitHash);
    }

    private static async Task<IReadOnlyList<BlameInfo>?> RunBlameAsync(string filePath, ITextSnapshot snapshot)
    {
        string? directory = Path.GetDirectoryName(filePath);
        if (directory == null || !Directory.Exists(directory))
            return null;

        string arguments = $"blame --porcelain --contents - -- \"{Path.GetFileName(filePath)}\"";
        string? output = await GitProcess.RunAsync(directory, arguments, snapshot.GetText());

        return output == null ? null : BlameParser.Parse(output);
    }

    private sealed class CachedBlame(string filePath, int version, Task<IReadOnlyList<BlameInfo>?> blame)
    {
        public string FilePath { get; } = filePath;

        public int Version { get; } = version;

        public Task<IReadOnlyList<BlameInfo>?> Blame { get; } = blame;
    }
}
