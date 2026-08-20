// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Windows;
using OTCode.Core.Abstractions.UI;

namespace OTCode.UI.Services;

public sealed class UIDispatcher : IUIDispatcher
{
    public void Post(Action action)
    {
        var dispatcher = Application.Current!.Dispatcher;

        if (dispatcher.CheckAccess())
            action();
        else
            dispatcher.BeginInvoke(() => action());
    }
}