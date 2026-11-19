using OpenGitBlame.Core;

namespace OpenGitBlame.UnitTests;

public class BlameFormatterTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

    private static BlameInfo Commit(DateTimeOffset time) =>
        new("a1b2c3d4e5f60718293a4b5c6d7e8f9012345678", "Ada Lovelace", "ada@example.com", time, "Add the engine");

    [Fact]
    public void Format_DefaultFormat()
    {
        string text = BlameFormatter.Format(BlameFormatter.DefaultFormat, Commit(Now.AddDays(-3)), Now);

        Assert.Equal("Ada Lovelace, 3 days ago • Add the engine", text);
    }

    [Fact]
    public void Format_ReplacesEveryPlaceholder()
    {
        string text = BlameFormatter.Format("{hash} {author} <{email}> {date} {summary} {unknown}", Commit(Now), Now);

        Assert.Equal("a1b2c3d Ada Lovelace <ada@example.com> 2026-06-01 Add the engine {unknown}", text);
    }

    [Fact]
    public void Format_UncommittedLine()
    {
        var info = new BlameInfo(new string('0', 40), "Not Committed Yet", "", Now, "");

        Assert.Equal(BlameFormatter.UncommittedText, BlameFormatter.Format(BlameFormatter.DefaultFormat, info, Now));
    }

    [Theory]
    [InlineData(0, "just now")]
    [InlineData(1, "1 minute ago")]
    [InlineData(59, "59 minutes ago")]
    [InlineData(60, "1 hour ago")]
    [InlineData(60 * 24, "1 day ago")]
    [InlineData(60 * 24 * 7, "1 week ago")]
    [InlineData(60 * 24 * 29, "4 weeks ago")]
    [InlineData(60 * 24 * 60, "2 months ago")]
    [InlineData(60 * 24 * 365 * 3, "3 years ago")]
    public void TimeAgo(int minutesAgo, string expected)
    {
        Assert.Equal(expected, BlameFormatter.TimeAgo(Now.AddMinutes(-minutesAgo), Now));
    }
}
