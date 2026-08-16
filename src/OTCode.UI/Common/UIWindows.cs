// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Windows;

namespace OTCode.UI.Common;

internal static class UIWindow
{
    internal static Window? Active
        => Application.Current is { } app
            ? app.Windows.OfType<Window>().FirstOrDefault(window => window.IsActive) ?? app.MainWindow
            : null;
}