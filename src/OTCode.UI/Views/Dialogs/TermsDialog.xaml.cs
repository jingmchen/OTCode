// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Logging;
using OTCode.Core.Logging;
using OTCode.UI.Utils;

namespace OTCode.UI.Views.Dialogs;

public sealed partial class TermsDialog : Window
{
    private readonly ILogger<TermsDialog> _logger;
    private ScrollViewer? _scroll;
    private bool _reachedEnd;

    public TermsDialog(ILogger<TermsDialog> logger, string termsConditionText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(termsConditionText);

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        InitializeComponent();
        TermsViewer.Document = MarkdownRenderer.RenderDocument(termsConditionText);
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
            LogUnableToLocateScrollViewer();
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

        _scroll?.ScrollChanged -= OnTermsScrollChanged;
    }

    private void OnAccept(object? sender, RoutedEventArgs e)
        => DialogResult = true;

    private void OnDecline(object? sender, RoutedEventArgs e)
        => DialogResult = false;
    
    [LoggerMessage(
        EventId = LogEventIDs.UI.TermsService.UnableToLocateScrollViewer,
        Level = LogLevel.Warning,
        Message = "Could not locate Scroll Viewer part.")]
    private partial void LogUnableToLocateScrollViewer();
}