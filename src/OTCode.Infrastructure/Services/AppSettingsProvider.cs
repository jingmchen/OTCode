// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Microsoft.Extensions.Logging;
using OTCode.Core.Abstractions.Infrastructure;
using OTCode.Core.Configuration.AppSettings;
using OTCode.Core.Enums;

namespace OTCode.Infrastructure.Services;

public sealed partial class AppSettingsProvider : SettingsProvider<AppSettings>
{
    private const int MinRetainedFiles = 1;
    private const int MaxRetainedFiles = 30;

    public AppSettingsProvider(ILogger<AppSettingsProvider> logger, IAppPaths appPaths)
        : base(logger, appPaths.UserAppSettingsFile)
    {
    }

    // ─── OVERWRITTEN METHODS ───────────────────
    protected override AppSettings Sanitize(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        settings.Logging ??= new();
        settings.Theme ??= new();

        if (!Enum.IsDefined(settings.Logging.MinimumLevel))
            settings.Logging.MinimumLevel = LogLevel.Information;
        
        settings.Logging.RetainedFileCountLimit =
            Math.Clamp(settings.Logging.RetainedFileCountLimit, MinRetainedFiles, MaxRetainedFiles);
        
        if (!Enum.IsDefined(settings.Theme.Theme))
            settings.Theme.Theme = AppTheme.Light;
        
        if (!Enum.IsDefined(settings.Theme.Accent))
            settings.Theme.Accent = AppAccent.Black;
        
        return settings;
    }
}