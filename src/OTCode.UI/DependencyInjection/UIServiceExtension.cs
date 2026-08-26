// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Microsoft.Extensions.DependencyInjection;
using Serilog.Core;
using OTCode.Core.Abstractions.UI;
using OTCode.UI.Services;
using OTCode.UI.ViewModels;
using OTCode.UI.Views;
using OTCode.UI.Views.Dialogs;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using OTCode.Core.Options.FileExplorer;

namespace OTCode.UI.DependencyInjection;

public static class UIServiceExtension
{
    public static IServiceCollection AddUIServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        
        services.AddSingleton<App>();
        
        services.AddConsoleLog();
        services.AddWindows();

        services.AddSingleton<IFilePickerService, FilePickerService>();
        services.AddSingleton<IHoverTracker, HoverTracker>();
        services.AddSingleton<IResourceUriProvider, ResourceUriProvider>();
        services.AddSingleton<ITermsService, TermsService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<IHoverTracker, HoverTracker>();
        services.AddSingleton<IUIDispatcher, UIDispatcher>();
        services.TryAddSingleton<IFileExplorerItemActions, FileExplorerItemActions>();
        services.AddSingleton<FileExplorerOptions>();
        return services;
    }

    private static IServiceCollection AddConsoleLog(this IServiceCollection services)
    {
        services.AddSingleton<ConsoleLog>();
        services.AddSingleton<IConsoleLog>(sp => sp.GetRequiredService<ConsoleLog>());
        services.AddSingleton<ILogEventSink>(sp => sp.GetRequiredService<ConsoleLog>());
        return services;
    }

    private static IServiceCollection AddWindows(this IServiceCollection services)
    {
        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainWindowViewModel>();
        return services;
    }
}