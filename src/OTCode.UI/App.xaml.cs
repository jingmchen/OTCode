// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OTCode.UI.Views;

namespace OTCode.UI;

public sealed partial class App : Application
{
    private readonly IServiceProvider _services;
    private readonly ILogger<App> _logger;

    public App(IServiceProvider services, ILogger<App> logger)
    {
        _services = services;
        _logger = logger;
        DispatcherUnhandledException += OnDispatcherUnhandledException;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        if (_services is { } services)
        {
            var themeService = _services.GetRequiredService<IThemeService>();
            themeService.Initialize();
        }
        base.OnStartup(e);
        MainWindow = _services.GetRequiredService<MainWindow>();
        MainWindow.Show();
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _logger.LogError(e.Exception, "Unhandled UI exception.");
        MessageBox.Show(
            "An unexpected error occurred. See the log file for details.",
            "CLX Transpiler",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }
}