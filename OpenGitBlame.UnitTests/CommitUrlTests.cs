using OpenGitBlame.Core;

namespace OpenGitBlame.UnitTests;

public class CommitUrlTests
{
    private const string Hash = "abc123";

    [Theory]
    [InlineData("https://github.com/owner/repo.git", "https://github.com/owner/repo/commit/abc123")]
    [InlineData("https://github.com/owner/repo", "https://github.com/owner/repo/commit/abc123")]
    [InlineData("git@github.com:owner/repo.git", "https://github.com/owner/repo/commit/abc123")]
    [InlineData("ssh://git@github.com:22/owner/repo.git", "https://github.com/owner/repo/commit/abc123")]
    [InlineData("git@gitlab.com:group/sub/repo.git", "https://gitlab.com/group/sub/repo/-/commit/abc123")]
    [InlineData("https://user@bitbucket.org/team/repo.git", "https://bitbucket.org/team/repo/commits/abc123")]
    [InlineData("https://org@dev.azure.com/org/project/_git/repo", "https://dev.azure.com/org/project/_git/repo/commit/abc123")]
    [InlineData("git@ssh.dev.azure.com:v3/org/project/repo", "https://dev.azure.com/org/project/_git/repo/commit/abc123")]
    public void From_KnownHosts(string remote, string expected)
    {
        Assert.Equal(expected, CommitUrl.From(remote, Hash));
    }

    [Theory]
    [InlineData(@"C:\repos\shared")]
    [InlineData("file:///C:/repos/shared")]
    [InlineData("")]
    public void From_LocalRemote_ReturnsNull(string remote)
    {
        Assert.Null(CommitUrl.From(remote, Hash));
    }
}
