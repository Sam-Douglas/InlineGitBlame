using OpenGitBlame.Core;
using OpenGitBlame.Git;
using OpenGitBlame.Options;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;

namespace OpenGitBlame.Commands;

/// <summary>
/// The Extensions > Open Git Blame menu and the editor's right-click commands. The ids match <c>OpenGitBlame.vsct</c>.
/// </summary>
internal sealed class BlameCommands(
    AsyncPackage package,
    GeneralOptionsPage optionsPage,
    BlameSettings settings,
    BlameProvider provider,
    IVsEditorAdaptersFactoryService editorAdapters,
    ITextDocumentFactoryService documentFactory)
{
    public static readonly Guid CommandSet = new("8545fe04-0d13-4a4f-a59e-9742a8a39c43");

    public const int EnabledCommandId = 0x0100;
    public const int OpenCommitCommandId = 0x0101;
    public const int CopyHashCommandId = 0x0102;
    public const int OpenSettingsCommandId = 0x0103;

    public void Register(IMenuCommandService commandService)
    {
        var enabled = new OleMenuCommand((_, _) => ToggleEnabled(), new CommandID(CommandSet, EnabledCommandId));
        enabled.BeforeQueryStatus += (_, _) => enabled.Checked = settings.Enabled;
        commandService.AddCommand(enabled);

        AddCommitCommand(commandService, OpenCommitCommandId, OpenCommitAsync);
        AddCommitCommand(commandService, CopyHashCommandId, CopyHashAsync);

        commandService.AddCommand(new MenuCommand(
            (_, _) => package.ShowOptionPage(typeof(GeneralOptionsPage)),
            new CommandID(CommandSet, OpenSettingsCommandId)));
    }

    /// <summary>The commit commands are greyed out while the extension is switched off.</summary>
    private void AddCommitCommand(IMenuCommandService commandService, int id, Func<Task> action)
    {
        var command = new OleMenuCommand((_, _) => Run(action), new CommandID(CommandSet, id));
        command.BeforeQueryStatus += (_, _) => command.Enabled = settings.Enabled;
        commandService.AddCommand(command);
    }

    private void ToggleEnabled()
    {
        optionsPage.Enabled = !optionsPage.Enabled;
        optionsPage.SaveSettingsToStorage();
        settings.Update(optionsPage.Enabled, optionsPage.Format);
    }

    private async Task OpenCommitAsync()
    {
        var caret = await GetCaretBlameAsync();
        if (caret == null) return;

        var (filePath, info) = caret.Value;
        string? url = await provider.GetCommitUrlAsync(filePath, info.CommitHash);

        await package.JoinableTaskFactory.SwitchToMainThreadAsync();
        if (url == null)
        {
            ShowMessage($"Commit {info.ShortHash} has no web page: the repository has no remote that can be opened in a browser.");
            return;
        }

        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    private async Task CopyHashAsync()
    {
        var caret = await GetCaretBlameAsync();
        if (caret == null) return;

        await package.JoinableTaskFactory.SwitchToMainThreadAsync();
        Clipboard.SetText(caret.Value.Info.CommitHash);
    }

    /// <summary>Finds the commit for the caret's line in the active editor, telling the user when there isn't one.</summary>
    private async Task<(string FilePath, BlameInfo Info)?> GetCaretBlameAsync()
    {
        await package.JoinableTaskFactory.SwitchToMainThreadAsync();

        if (await package.GetServiceAsync(typeof(SVsTextManager)) is not IVsTextManager textManager
            || textManager.GetActiveView(1, null, out var activeView) != 0
            || editorAdapters.GetWpfTextView(activeView) is not { } view
            || !documentFactory.TryGetTextDocument(view.TextDataModel.DocumentBuffer, out var document))
        {
            return null;
        }

        var caret = view.Caret.Position.Point.GetPoint(document.TextBuffer, PositionAffinity.Successor);
        if (caret == null) return null;

        var blame = await provider.GetBlameAsync(document.FilePath, caret.Value.Snapshot);
        await package.JoinableTaskFactory.SwitchToMainThreadAsync();

        int lineNumber = caret.Value.GetContainingLine().LineNumber;
        if (blame == null || lineNumber >= blame.Count)
        {
            ShowMessage("This file isn't tracked by git.");
            return null;
        }

        var info = blame[lineNumber];
        if (!info.IsCommitted)
        {
            ShowMessage("This line hasn't been committed yet.");
            return null;
        }

        return (document.FilePath, info);
    }

    private void ShowMessage(string message) =>
        VsShellUtilities.ShowMessageBox(package, message, "Open Git Blame", OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

    private void Run(Func<Task> action) =>
        package.JoinableTaskFactory.RunAsync(action).FileAndForget("OpenGitBlame/Command");
}
