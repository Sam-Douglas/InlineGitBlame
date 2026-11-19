using OpenGitBlame.Core;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace OpenGitBlame.Options;

/// <summary>
/// Tools > Options > Open Git Blame > General.
/// </summary>
[ComVisible(true)]
[Guid("e202e854-9b14-4df3-9fab-c89655728b88")]
public class GeneralOptionsPage : DialogPage
{
    public const string CategoryName = "Open Git Blame";
    public const string PageName = "General";

    /// <summary>Raised after the user confirms the Options dialog.</summary>
    public event EventHandler? Applied;

    [Category("General")]
    [DisplayName("Enabled")]
    [Description("When off, no blame is shown and git isn't run.")]
    [DefaultValue(true)]
    public bool Enabled { get; set; } = true;

    [Category("Inline blame")]
    [DisplayName("Message format")]
    [Description("The text shown after the line. Placeholders: {author}, {email}, {date}, {ago}, {summary} and {hash}.")]
    [DefaultValue(BlameFormatter.DefaultFormat)]
    public string Format { get; set; } = BlameFormatter.DefaultFormat;

    protected override void OnApply(PageApplyEventArgs e)
    {
        base.OnApply(e);

        if (e.ApplyBehavior == ApplyKind.Apply)
            Applied?.Invoke(this, EventArgs.Empty);
    }
}
