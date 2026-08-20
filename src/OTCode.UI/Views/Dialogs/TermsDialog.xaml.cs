// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Logging;
using OTCode.UI.Utils;

namespace OTCode.UI.Views.Dialogs;

public sealed partial class TermsDialog : Window
{
    private readonly ILogger<TermsDialog> _logger;
    private ScrollViewer? _scroll;
    private bool _reachedEnd;
    private bool _accepted;

    public TermsDialog(string termsConditionText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(termsConditionText);

        InitializeComponent();
        TermsViewer.Document = MarkdownRenderer.Render(termsConditionText);
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;

        // FlowDocumentScrollViewer has no scroll events or offset properties of
        // its own — the real ScrollViewer lives inside its template as the
        // standard "PART_ContentHost" part, so dig it out after the template
        // has been applied.
        TermsViewer.ApplyTemplate();
        _scroll = TermsViewer.Template?.FindName("PART_ContentHost", TermsViewer) as ScrollViewer;

        if (_scroll is null)
        {
            _logger.LogError("error");
            // Template part missing (e.g. a restyled control) — fail open
            // rather than locking the user out of accepting.
            ActivateAcceptButton();
            return;
        }

        _scroll.ScrollChanged += OnTermsScrollChanged;
        CheckReachedEnd();
    }

    private void OnTermsScrollChanged(object sender, ScrollChangedEventArgs e)
        => CheckReachedEnd();

    private void CheckReachedEnd()
    {
        if (_reachedEnd || _scroll is null)
            return;

        // ExtentHeight == 0 means the FlowDocument hasn't been formatted yet,
        // so "nothing to scroll" can't be trusted at that moment. ScrollChanged
        // fires again once the content gets its real size.
        if (_scroll.ExtentHeight == 0)
            return;

        bool fitsWithoutScrolling = _scroll.ScrollableHeight == 0;
        bool scrolledToBottom = _scroll.VerticalOffset >= _scroll.ScrollableHeight - 2;

        if (fitsWithoutScrolling || scrolledToBottom)
            ActivateAcceptButton();
    }

    private void ActivateAcceptButton()
    {
        // Latched: scrolling back up afterwards doesn't re-disable Accept.
        _reachedEnd = true;
        AcceptButton.IsEnabled = true;

        _scrollViewer?.ScrollChanged -= OnTermsScrollChanged;
    }

    private void OnAccept(object? sender, RoutedEventArgs e)
        => DialogResult = true;

    private void OnDecline(object? sender, RoutedEventArgs e)
        => DialogResult = false;
}