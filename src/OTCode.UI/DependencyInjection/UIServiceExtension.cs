// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Microsoft.Extensions.DependencyInjection;

namespace SimplyDraft.UI.DependencyInjection;

public static class UIServiceExtension
{
    public static IServiceCollection AddUIServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services;
    }
}