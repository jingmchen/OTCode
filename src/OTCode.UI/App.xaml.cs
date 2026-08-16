// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OTCode.Core.Abstractions.Infrastructure;
using OTCode.Core.Abstractions.UI;
using OTCode.UI.Views;

namespace OTCode.UI;

public sealed partial class App : Application
{
    private readonly IServiceProvider _services;
    private readonly IAtomicFileAsync _fileWriter;
    private readonly ILogger<App> _logger;

    public App(IServiceProvider services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _fileWriter = services.GetRequiredService<IAtomicFileAsync>();
        _logger = services.GetRequiredService<ILogger<App>>();

        DispatcherUnhandledException += OnDispatcherUnhandledException;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (_services is { } services)
        {
            var themeService = services.GetRequiredService<IThemeService>();
            themeService.Initialize();

            ShutdownMode = ShutdownMode.OnLastWindowClose;

            MainWindow = _services.GetRequiredService<MainWindow>();
            MainWindow.Show();
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        try
        {
            _fileWriter.FlushAsync().GetAwaiter().GetResult();
        }
        finally
        {
            base.OnExit(e);
        }
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