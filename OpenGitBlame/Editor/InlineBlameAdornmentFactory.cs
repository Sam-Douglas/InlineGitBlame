using OpenGitBlame.Git;
using OpenGitBlame.Options;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace OpenGitBlame.Editor;

[Export(typeof(IWpfTextViewCreationListener))]
[ContentType("text")]
[TextViewRole(PredefinedTextViewRoles.PrimaryDocument)]
internal sealed class InlineBlameAdornmentFactory : IWpfTextViewCreationListener
{
    [Export(typeof(AdornmentLayerDefinition))]
    [Name(InlineBlameAdornment.LayerName)]
    [Order(After = PredefinedAdornmentLayers.Text)]
    public AdornmentLayerDefinition? LayerDefinition { get; set; }

    [Import]
    public ITextDocumentFactoryService DocumentFactory { get; set; } = null!;

    [Import]
    public BlameProvider BlameProvider { get; set; } = null!;

    [Import]
    public BlameSettings Settings { get; set; } = null!;

    [Import]
    public JoinableTaskContext JoinableTaskContext { get; set; } = null!;

    public void TextViewCreated(IWpfTextView textView)
    {
        // Only files on disk can be blamed.
        if (!DocumentFactory.TryGetTextDocument(textView.TextDataModel.DocumentBuffer, out var document))
            return;

        textView.Properties.GetOrCreateSingletonProperty(() => new InlineBlameAdornment(textView, document, BlameProvider, Settings, JoinableTaskContext.Factory));
    }
}
