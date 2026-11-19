using OpenGitBlame.Core;
using OpenGitBlame.Git;
using OpenGitBlame.Options;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace OpenGitBlame.Editor;

/// <summary>
/// Shows the blame for the caret's line, faded, after the end of the line.
/// </summary>
internal sealed class InlineBlameAdornment
{
    public const string LayerName = "OpenGitBlame.InlineBlame";

    // Waits for typing and caret movement to settle before running git.
    private static readonly TimeSpan Delay = TimeSpan.FromMilliseconds(250);

    private readonly IWpfTextView _view;
    private readonly ITextDocument _document;
    private readonly BlameProvider _provider;
    private readonly BlameSettings _settings;
    private readonly JoinableTaskFactory _joinableTaskFactory;
    private readonly IAdornmentLayer _layer;
    private readonly DispatcherTimer _timer;

    private ITextSnapshot? _blameSnapshot;
    private IReadOnlyList<BlameInfo>? _blame;
    private bool _stale;
    private int _lastRequest;

    public InlineBlameAdornment(IWpfTextView view, ITextDocument document, BlameProvider provider, BlameSettings settings, JoinableTaskFactory joinableTaskFactory)
    {
        _view = view;
        _document = document;
        _provider = provider;
        _settings = settings;
        _joinableTaskFactory = joinableTaskFactory;
        _layer = view.GetAdornmentLayer(LayerName);
        _timer = new DispatcherTimer(Delay, DispatcherPriority.Background, OnTimerTick, view.VisualElement.Dispatcher);
        _timer.Stop();

        _view.Caret.PositionChanged += OnCaretPositionChanged;
        _view.LayoutChanged += OnLayoutChanged;
        _view.GotAggregateFocus += OnGotFocus;
        _view.Closed += OnClosed;
        _document.TextBuffer.Changed += OnTextChanged;
        _document.FileActionOccurred += OnFileActionOccurred;
        _settings.Changed += OnSettingsChanged;

        Refresh();
    }

    /// <summary>Draws the blame for the caret's line, fetching it first if the text has changed.</summary>
    private void Refresh()
    {
        _layer.RemoveAllAdornments();

        if (!_settings.Enabled || _view.IsClosed)
            return;

        var caret = CaretInDocument();
        if (caret == null)
            return;

        if (caret.Value.Snapshot != _blameSnapshot || _stale)
        {
            _timer.Stop();
            _timer.Start();
        }

        // Once the text changes the old line numbers no longer match.
        if (caret.Value.Snapshot != _blameSnapshot)
            return;

        int lineNumber = caret.Value.GetContainingLine().LineNumber;
        if (_blame == null || lineNumber >= _blame.Count || _blame[lineNumber].CommitHash.Length == 0)
            return;

        Draw(_blame[lineNumber]);
    }

    private void Draw(BlameInfo info)
    {
        if (_view.FormattedLineSource == null)
            return;

        // With word wrap on, the message goes after the last row of the line.
        var caretLine = _view.Caret.Position.BufferPosition.GetContainingLine();
        var viewLine = _view.GetTextViewLineContainingBufferPosition(caretLine.End);
        if (viewLine == null || viewLine.VisibilityState == VisibilityState.Unattached)
            return;

        var textProperties = _view.FormattedLineSource.DefaultTextProperties;
        var label = new TextBlock
        {
            Text = BlameFormatter.Format(_settings.Format, info, DateTimeOffset.Now),
            FontFamily = textProperties.Typeface.FontFamily,
            FontSize = textProperties.FontRenderingEmSize,
            FontStyle = FontStyles.Italic,
            Foreground = textProperties.ForegroundBrush,
            Opacity = 0.5,
            ToolTip = ToolTipText(info),
        };

        Canvas.SetLeft(label, viewLine.TextRight + textProperties.FontRenderingEmSize * 2);
        Canvas.SetTop(label, viewLine.TextTop);

        _layer.AddAdornment(AdornmentPositioningBehavior.TextRelative, viewLine.Extent, null, label, null);
    }

    private static string ToolTipText(BlameInfo info)
    {
        if (!info.IsCommitted)
            return BlameFormatter.UncommittedText;

        string ago = BlameFormatter.TimeAgo(info.AuthorTime, DateTimeOffset.Now);
        return $"{info.Author} <{info.AuthorEmail}>\n" +
               $"{info.AuthorTime:f} ({ago})\n" +
               $"{info.ShortHash}\n\n" +
               info.Summary;
    }

    private SnapshotPoint? CaretInDocument() =>
        _view.Caret.Position.Point.GetPoint(_document.TextBuffer, PositionAffinity.Successor);

    private async Task FetchBlameAsync()
    {
        var caret = CaretInDocument();
        if (caret == null)
            return;

        int request = ++_lastRequest;
        var snapshot = caret.Value.Snapshot;
        var blame = await _provider.GetBlameAsync(_document.FilePath, snapshot);

        await _joinableTaskFactory.SwitchToMainThreadAsync();
        if (request != _lastRequest)
            return;

        _blameSnapshot = snapshot;
        _blame = blame;
        _stale = false;
        Refresh();
    }

    private void OnTimerTick(object sender, EventArgs e)
    {
        _timer.Stop();
        _joinableTaskFactory.RunAsync(FetchBlameAsync).FileAndForget("OpenGitBlame/FetchBlame");
    }

    private void OnCaretPositionChanged(object sender, CaretPositionChangedEventArgs e) => Refresh();

    private void OnTextChanged(object sender, TextContentChangedEventArgs e) => Refresh();

    private void OnSettingsChanged(object sender, EventArgs e) => Refresh();

    private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
    {
        // Adornments on re-laid-out lines are removed by the editor, so put it back.
        var caretLineEnd = _view.Caret.Position.BufferPosition.GetContainingLine().End;
        if (e.NewOrReformattedLines.Any(line => line.ContainsBufferPosition(caretLineEnd)))
            Refresh();
    }

    // A commit, pull or checkout outside Visual Studio usually happens while the editor isn't focused.
    private void OnGotFocus(object sender, EventArgs e) => ForgetBlame();

    private void OnFileActionOccurred(object sender, TextDocumentFileActionEventArgs e)
    {
        if (e.FileActionType.HasFlag(FileActionTypes.ContentSavedToDisk) || e.FileActionType.HasFlag(FileActionTypes.DocumentRenamed))
            ForgetBlame();
    }

    private void ForgetBlame()
    {
        _provider.Invalidate(_document.TextBuffer);
        _stale = true;
        Refresh();
    }

    private void OnClosed(object sender, EventArgs e)
    {
        _timer.Stop();
        _view.Caret.PositionChanged -= OnCaretPositionChanged;
        _view.LayoutChanged -= OnLayoutChanged;
        _view.GotAggregateFocus -= OnGotFocus;
        _view.Closed -= OnClosed;
        _document.TextBuffer.Changed -= OnTextChanged;
        _document.FileActionOccurred -= OnFileActionOccurred;
        _settings.Changed -= OnSettingsChanged;
    }
}
