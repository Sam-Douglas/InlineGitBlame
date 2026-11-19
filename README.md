# Open Git Blame

A small Visual Studio extension that shows who last changed the line you're on, and when, faded out at the end of the line:

```
var total = items.Sum(i => i.Price);      Ada Lovelace, 3 days ago • Add basket totals
```

Hover over the message for the full author, date and commit.

## Features

- **Inline blame** for the caret's line, updated as you move and type. Lines you've changed but not committed show as *You, uncommitted changes*.
- **Open Line Commit in Browser**: opens the commit on GitHub, GitLab, Bitbucket or Azure DevOps.
- **Copy Line Commit Hash**.

Both commands are on the editor's right-click menu and under **Extensions > Open Git Blame**, along with an **Enabled** toggle that switches the extension off without uninstalling it.

## Settings

**Tools > Options > Open Git Blame > General**

| Setting | Default |
| --- | --- |
| Enabled | On |
| Message format | `{author}, {ago} • {summary}` |

The format accepts `{author}`, `{email}`, `{date}`, `{ago}`, `{summary}` and `{hash}`.

## Requirements

Visual Studio 2022 (17.14) or later, and `git` on your `PATH`.

## Building

Open `OpenGitBlame.slnx` and press F5 to start the experimental instance, or run:

```bash
dotnet test OpenGitBlame.UnitTests
```

## Projects

- `OpenGitBlame.Core`: parsing `git blame --porcelain`, formatting the message and building commit URLs. No Visual Studio dependencies.
- `OpenGitBlame`: the VSIX: the editor adornment, commands and options page.
- `OpenGitBlame.UnitTests`: tests for the core library.
