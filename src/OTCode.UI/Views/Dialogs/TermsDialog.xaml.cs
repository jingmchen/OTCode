// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Windows;
using OTCode.UI.Utils;

namespace OTCode.UI.Views.Dialogs;

public sealed partial class TermsDialog : Window
{
    private readonly TaskCompletionSource<bool> _result = new();
    private bool _accepted;

    public TermsDialog()
        => InitializeComponent();

    public static Task<bool> ShowStandaloneAsync(string termsConditionText)
    {
        ArgumentNullException.ThrowIfNull(termsConditionText);

        var dialog = new TermsDialog();
        
        dialog.TermsHost.Content = MarkdownRenderer.Render(termsConditionText);
        dialog.Show();
        
        return dialog._result.Task;
    }

    protected override void OnClosed(EventArgs e)
    {
        _result.TrySetResult(_accepted);
        base.OnClosed(e);
    }

    private void OnAccept(object? sender, RoutedEventArgs e)
    {
        _accepted = true;
        Close();
    }

    private void OnDecline(object? sender, RoutedEventArgs e)
        => Close();
}