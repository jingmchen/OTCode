// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Configuration;

namespace OTCode.UI.Services;

public static class ConsoleLogExtension
{
    public static LoggerConfiguration ConsoleLogPane(this LoggerSinkConfiguration writeTo, IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(writeTo);
        ArgumentNullException.ThrowIfNull(services);

        return writeTo.Sink(services.GetRequiredService<ConsoleLog>());
    }
}