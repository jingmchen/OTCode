// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Microsoft.Extensions.DependencyInjection;
using OTCode.Core.Abstractions.UI;
using OTCode.UI.Services;

namespace OTCode.UI.DependencyInjection;

public static class UIServiceExtension
{
    public static IServiceCollection AddUIServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IUriPaths, UriPaths>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<IHoverTracker, HoverTracker>();
        return services;
    }
}