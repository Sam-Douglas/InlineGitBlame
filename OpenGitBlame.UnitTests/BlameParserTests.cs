using OpenGitBlame.Core;

namespace OpenGitBlame.UnitTests;

public class BlameParserTests
{
    private const string HashA = "a1b2c3d4e5f60718293a4b5c6d7e8f9012345678";
    private const string HashB = "ffffffffffffffffffffffffffffffffffffffff";
    private const string Uncommitted = "0000000000000000000000000000000000000000";

    // A commit's details are only written the first time it appears.
    private static readonly string Porcelain = string.Join("\n",
        $"{HashA} 1 1 2",
        "author Ada Lovelace",
        "author-mail <ada@example.com>",
        "author-time 1700000000",
        "author-tz +0100",
        "committer Ada Lovelace",
        "committer-mail <ada@example.com>",
        "committer-time 1700000000",
        "committer-tz +0100",
        "summary Add the engine",
        "filename Engine.cs",
        "\tclass Engine",
        $"{HashA} 2 2",
        "\t{",
        $"{HashB} 5 3 1",
        "author Charles Babbage",
        "author-mail <charles@example.com>",
        "author-time 1710000000",
        "author-tz -0500",
        "summary Fix the gears",
        "previous 1111111111111111111111111111111111111111 Engine.cs",
        "filename Engine.cs",
        "\t    void Turn() { }",
        $"{Uncommitted} 4 4 1",
        "author Not Committed Yet",
        "author-mail <not.committed.yet>",
        "author-time 1720000000",
        "author-tz +0000",
        "summary Version of Engine.cs from Engine.cs",
        "filename Engine.cs",
        "\t    // todo",
        $"{HashA} 3 5 1",
        "filename Engine.cs",
        "\t}");

    [Fact]
    public void Parse_ReturnsOneEntryPerLine()
    {
        var lines = BlameParser.Parse(Porcelain);

        Assert.Equal(5, lines.Count);
        Assert.Equal([HashA, HashA, HashB, Uncommitted, HashA], lines.Select(l => l.CommitHash));
    }

    [Fact]
    public void Parse_ReadsCommitDetails()
    {
        var line = BlameParser.Parse(Porcelain)[2];

        Assert.Equal("Charles Babbage", line.Author);
        Assert.Equal("charles@example.com", line.AuthorEmail);
        Assert.Equal("Fix the gears", line.Summary);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1710000000), line.AuthorTime);
        Assert.Equal(TimeSpan.FromHours(-5), line.AuthorTime.Offset);
    }

    [Fact]
    public void Parse_ReusesDetailsForRepeatedCommits()
    {
        var lines = BlameParser.Parse(Porcelain);

        Assert.Same(lines[0], lines[4]);
        Assert.Equal("Ada Lovelace", lines[4].Author);
    }

    [Fact]
    public void Parse_MarksWorkingCopyLinesAsUncommitted()
    {
        var lines = BlameParser.Parse(Porcelain);

        Assert.False(lines[3].IsCommitted);
        Assert.True(lines[0].IsCommitted);
    }

    [Fact]
    public void Parse_EmptyOutput_ReturnsNoLines()
    {
        Assert.Empty(BlameParser.Parse(""));
    }
}
