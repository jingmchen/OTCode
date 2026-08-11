// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using OTCode.Core.Abstractions.Infrastructure;
using OTCode.Core.Configuration;
using OTCode.Core.Enums;
using OTCode.Infrastructure.Logging;
using OTCode.Infrastructure.Utils;

namespace OTCode.Infrastructure.Services;

public sealed partial class AppSettingsProvider : IAppSettingsProvider
{
    private readonly ILogger<AppSettingsProvider> _logger;
    private readonly object _gate = new();
    private readonly string _settingsPath;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = {new JsonStringEnumConverter()}
    };
    private const int MinRetainedFiles = 1;
    private const int MaxRetainedFiles = 30;
    public AppSettings Current {get; private set;} = null!;

    public AppSettingsProvider(ILogger<AppSettingsProvider> logger, IAppPaths appPaths)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settingsPath = appPaths.UserAppSettingsFile ?? throw new ArgumentNullException(nameof(appPaths));

        var dir = Path.GetDirectoryName(_settingsPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        
        Reload();
    }

    // ─── PUBLIC METHODS ────────────────────────
    public void Save()
    {
        lock(_gate)
            WriteToDisk(Current);
    }

    public void Reload()
    {
        lock(_gate)
            ReloadCore();
    }

    // ─── PRIVATE METHODS ───────────────────────
    private void ReloadCore()
    {
        if (!File.Exists(_settingsPath))
        {
            _logger.LogCreatingDefaults();
            ApplyDefaults();
            return;
        }

        try
        {
            var json = File.ReadAllText(_settingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);

            if (settings is null)
            {
                _logger.LogInvalidContent();
                ApplyDefaults();
                return;
            }

            Current = Sanitize(settings);
            Save();
        }
        catch (Exception ex)
        {
            _logger.LogUnableToLoad(ex);
            ApplyDefaults();
        }
    }

    private void ApplyDefaults()
    {
        Current = new AppSettings();
        Save();
    }

    private void WriteToDisk(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, _jsonOptions);

        try
        {
            AtomicFileWriter.WriteTo(_settingsPath, json);
        }
        catch (Exception ex)
        {
            _logger.LogUnableToSave(ex);
        }
    }

    private static AppSettings Sanitize(AppSettings settings)
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