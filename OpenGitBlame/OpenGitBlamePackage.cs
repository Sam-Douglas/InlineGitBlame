using OpenGitBlame.Commands;
using OpenGitBlame.Git;
using OpenGitBlame.Options;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace OpenGitBlame;

/// <summary>
/// Loads the settings into the editor components and registers the menu commands. The inline blame
/// itself is a MEF component (<see cref="Editor.InlineBlameAdornmentFactory"/>).
/// </summary>
[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[ProvideAutoLoad(VSConstants.UICONTEXT.ShellInitialized_string, PackageAutoLoadFlags.BackgroundLoad)]
[ProvideMenuResource("Menus.ctmenu", 1)]
[ProvideOptionPage(typeof(GeneralOptionsPage), GeneralOptionsPage.CategoryName, GeneralOptionsPage.PageName, 0, 0, true)]
[Guid(PackageGuidString)]
public sealed class OpenGitBlamePackage : AsyncPackage
{
    private const string PackageGuidString = "2e6e4f4f-a80a-4234-b678-c8d701dab045";

    private GeneralOptionsPage? _optionsPage;
    private BlameSettings? _settings;

    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        await base.InitializeAsync(cancellationToken, progress);

        if (await GetServiceAsync(typeof(SComponentModel)) is not IComponentModel componentModel)
            return;

        var commandService = await GetServiceAsync(typeof(IMenuCommandService)) as IMenuCommandService;

        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        _optionsPage = (GeneralOptionsPage)GetDialogPage(typeof(GeneralOptionsPage));
        _settings = componentModel.GetService<BlameSettings>();
        ApplySettings();
        _optionsPage.Applied += OnOptionsApplied;

        var commands = new BlameCommands(
            this,
            _optionsPage,
            _settings,
            componentModel.GetService<BlameProvider>(),
            componentModel.GetService<IVsEditorAdaptersFactoryService>(),
            componentModel.GetService<ITextDocumentFactoryService>());

        if (commandService != null)
            commands.Register(commandService);
    }

    private void OnOptionsApplied(object sender, EventArgs e) => ApplySettings();

    private void ApplySettings() => _settings?.Update(_optionsPage!.Enabled, _optionsPage.Format);

    protected override void Dispose(bool disposing)
    {
        if (disposing && _optionsPage != null)
            _optionsPage.Applied -= OnOptionsApplied;

        base.Dispose(disposing);
    }
}
