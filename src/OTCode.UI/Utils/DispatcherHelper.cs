// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Windows;

namespace OTCode.UI.Utils;

internal static class DispatcherHelper
{
    internal static void PostOnUIThread(Action action)
    {
        var dispatcher = Application.Current!.Dispatcher;

        if (dispatcher.CheckAccess())
            action();
        else
            dispatcher.BeginInvoke(() => action());
    }
}