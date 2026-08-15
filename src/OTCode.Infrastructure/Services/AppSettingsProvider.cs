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

        settings.LoggingSection ??= new();
        settings.ThemeSection ??= new();

        if (!Enum.IsDefined(settings.LoggingSection.MinimumLevel))
            settings.LoggingSection.MinimumLevel = LogLevel.Information;
        
        settings.LoggingSection.RetainedFileCountLimit =
            Math.Clamp(settings.LoggingSection.RetainedFileCountLimit, MinRetainedFiles, MaxRetainedFiles);
        
        if (!Enum.IsDefined(settings.ThemeSection.Theme))
            settings.ThemeSection.Theme = AppTheme.Light;
        
        if (!Enum.IsDefined(settings.ThemeSection.Accent))
            settings.ThemeSection.Accent = AppAccent.Black;
        
        return settings;
    }
}