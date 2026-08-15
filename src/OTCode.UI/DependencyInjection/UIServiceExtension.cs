// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Microsoft.Extensions.DependencyInjection;
using OTCode.Core.Abstractions.UI;
using OTCode.UI.Services;
using Serilog.Core;

namespace OTCode.UI.DependencyInjection;

public static class UIServiceExtension
{
    public static IServiceCollection AddUIServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        
        services.AddMisc();

        services.AddSingleton<IUriPaths, UriPaths>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<IHoverTracker, HoverTracker>();
        return services;
    }

    private static IServiceCollection AddMisc(this IServiceCollection services)
    {
        services.AddSingleton<ConsoleLog>();
        services.AddSingleton<IConsoleLog>(sp => sp.GetRequiredService<ConsoleLog>());
        services.AddSingleton<ILogEventSink>(sp => sp.GetRequiredService<ConsoleLog>());
        return services;
    }
}