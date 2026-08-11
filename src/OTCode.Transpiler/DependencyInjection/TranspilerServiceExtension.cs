// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Microsoft.Extensions.DependencyInjection;

namespace OTCode.Transpiler.DependencyInjection;

public static class TranspilerServiceExtension
{
    public static IServiceCollection AddTranspilerServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        
        return services;
    }
}