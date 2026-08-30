// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Serilog.Templates;
using OTCode.Transpiler.DependencyInjection;
using OTCode.Infrastructure.DependencyInjection;
using OTCode.Infrastructure.Services;
using OTCode.Infrastructure.Utils;
using OTCode.UI;
using OTCode.UI.DependencyInjection;
using OTCode.UI.Services;
using Serilog.Core;

namespace OTCode.AppHost;

internal sealed class Program
{
    internal const string LoggerFormat =
        "[{@t:dd-MMM-yyyy}] [{@t:HH:mm:ss}] [{@l:u3}]" +
        "{#if SourceContext is not null} [{SourceContext}]{#end}" +
        " [{@m}]" +
        "{#if @x is not null} [{@x}]{#end}" +
        "\n";
    
    [STAThread]
    internal static int Main(string[] args)
    {
        var appInfo = new AppInfo(typeof(Program).Assembly);
        var appPaths = new AppPaths(appInfo);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(formatter: new ExpressionTemplate(LoggerFormat))
            .CreateBootstrapLogger();
        
        var bootstrapLoggerFactory = new SerilogLoggerFactory();

        try
        {
            var logsHandler = new LogsHandler(
                logger: bootstrapLoggerFactory.CreateLogger<LogsHandler>(),
                logsFolderPath: appPaths.LogsFolder,
                latestLogFilePath: appPaths.LatestLogFile);
            
            logsHandler.ArchivePreviousLatestLogFile();

            var settings = new AppSettingsProvider(
                logger: bootstrapLoggerFactory.CreateLogger<AppSettingsProvider>(),
                appPaths: appPaths);
            
            using IHost host = BuildHost(args, appPaths, settings);
            
            host.Start();

            Log.Information(
                "Starting {AppName} version: {Version}",
                appInfo.Product,
                appInfo.InfoVersion);

            try
            {
                var app = host.Services.GetRequiredService<App>();
                return app.Run();
            }
            finally
            {
                host.StopAsync(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();
            }
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly.");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static IHost BuildHost(string[] args, AppPaths appPaths, AppSettingsProvider appSettings)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = args,
            ContentRootPath = AppContext.BaseDirectory
        });

        var level = Enum.Parse<LogEventLevel>(appSettings.Current.Logging.MinimumLevel.ToString());
        var levelSwitch = new LoggingLevelSwitch(level);

        builder.Services.AddSerilog((services, configuration) => configuration
            .MinimumLevel.ControlledBy(levelSwitch)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Debug(
                formatter: new ExpressionTemplate(LoggerFormat))
            .WriteTo.Console(
                formatter: new ExpressionTemplate(LoggerFormat))
            .WriteTo.File(
                path: appPaths.LatestLogFile,
                formatter: new ExpressionTemplate(LoggerFormat))
            .WriteTo.ConsoleLogPane(services));  
        
        builder.Services.AddSingleton(typeof(Program).Assembly);
        builder.Services.AddSingleton(levelSwitch);
        builder.Services.AddInfrastructureServices();
        builder.Services.AddTranspilerServices();
        builder.Services.AddUIServices();
        
        return builder.Build();
    }
}