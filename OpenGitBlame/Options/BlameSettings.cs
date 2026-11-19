using OpenGitBlame.Core;
using System;
using System.ComponentModel.Composition;

namespace OpenGitBlame.Options;

/// <summary>
/// The current settings, shared with the editor components. The package copies them in from the
/// options page when it loads and whenever they change.
/// </summary>
[Export]
public sealed class BlameSettings
{
    public bool Enabled { get; private set; } = true;

    public string Format { get; private set; } = BlameFormatter.DefaultFormat;

    /// <summary>Raised on the UI thread after a setting changes.</summary>
    public event EventHandler? Changed;

    public void Update(bool enabled, string format)
    {
        Enabled = enabled;
        Format = string.IsNullOrWhiteSpace(format) ? BlameFormatter.DefaultFormat : format;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
