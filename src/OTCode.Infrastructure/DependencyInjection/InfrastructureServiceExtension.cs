// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Microsoft.Extensions.DependencyInjection;

namespace OTCode.Infrastructure.DependencyInjection;

public static class InfrastructureServiceExtension
{   
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        
        return services;
    }
}