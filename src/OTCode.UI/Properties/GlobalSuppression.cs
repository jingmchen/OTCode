// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Performance", "CA1848",
    Justification = "Runs once on unhandled exception crash.",
    Scope = "member",
    Target = "~M:OTCode.UI.App.OnDispatcherUnhandledException(" +
        "System.Object," +
        "System.Windows.Threading.DispatcherUnhandledExceptionEventArgs)")]