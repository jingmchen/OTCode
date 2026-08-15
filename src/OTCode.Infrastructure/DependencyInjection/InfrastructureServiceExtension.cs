// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Microsoft.Extensions.DependencyInjection;
using OTCode.Core.Abstractions.Infrastructure;
using OTCode.Core.Configuration.AppSettings;
using OTCode.Core.Configuration.UserStateSettings;
using OTCode.Infrastructure.Services;

namespace OTCode.Infrastructure.DependencyInjection;

public static class InfrastructureServiceExtension
{   
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        
        services.AddSingleton<IAppInfo, AppInfo>();
        services.AddSingleton<IAppPaths, AppPaths>();
        services.AddSingleton<ISettingsProvider<AppSettings>, AppSettingsProvider>();
        services.AddSingleton<ISettingsProvider<UserStateSettings>, UserStateSettingsProvider>();
        services.AddSingleton<IAtomicFileAsync, AtomicFileAsync>();
        return services;
    }
}