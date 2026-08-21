// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using OTCode.Core.Abstractions.Infrastructure;
using OTCode.Core.Logging;
using OTCode.Infrastructure.Utils;

namespace OTCode.Infrastructure.Services;

public abstract partial class SettingsProvider<T> : ISettingsProvider<T> where T : class, new()
{
    private readonly ILogger _logger;
    private readonly object _gate = new();
    private readonly string _settingsPath;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = {new JsonStringEnumConverter()}
    };
    protected virtual bool EnableAdditionalLogging => true;
    public T Current {get; private set;} = null!;

    protected SettingsProvider(ILogger logger, string settingsPath)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settingsPath = settingsPath;

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

    public bool TrySave(out Exception? err)
    {
        try
        {
            Save();
            err = null;
            return true;
        }
        catch (Exception ex)
        {
            err = ex;
            LogFileUnableToSave(ex, Path.GetFileName(_settingsPath));
            return false;
        }
    }

    public void Reload()
    {
        lock(_gate)
            ReloadCore();
    }

    // ─── PRIVATE METHODS ───────────────────────
    private void WriteToDisk(T settings)
    {
        var json = JsonSerializer.Serialize(settings, _jsonOptions);
        AtomicFile.WriteTo(_settingsPath, json);
    }

    private void ReloadCore()
    {
        if (!File.Exists(_settingsPath))
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                var fileName = Path.GetFileName(_settingsPath);

                if (EnableAdditionalLogging)
                    LogFileNotFoundCreateDefaults(fileName);
            }
            ApplyDefaults();
            return;
        }

        try
        {
            var json = File.ReadAllText(_settingsPath);
            var settings = JsonSerializer.Deserialize<T>(json, _jsonOptions);

            if (settings is null)
            {
                LogFileInvalidOrEmptyCreateDefaults(Path.GetFileName(_settingsPath));
                ApplyDefaults();
                return;
            }

            Current = Sanitize(settings);
            Save();
        }
        catch (Exception ex)
        {
            LogFileUnableToReadCreateDefaults(ex, Path.GetFileName(_settingsPath));
            ApplyDefaults();
        }
    }

    private void ApplyDefaults()
    {
        Current = new T();
        Save();
    }

    protected abstract T Sanitize(T settings);

    [LoggerMessage(
        EventId = LogEventIDs.Infrastructure.SettingsProvider.FileNotFound,
        Level = LogLevel.Information,
        Message = "File: {FileName} - Unable to be found. Reverting to factory defaults.")]
    private partial void LogFileNotFoundCreateDefaults(string fileName);

    [LoggerMessage(
        EventId = LogEventIDs.Infrastructure.SettingsProvider.FileUnableToRead,
        Level = LogLevel.Warning,
        Message = "File: {FileName} - Unable to be read. Reverting to factory defaults.")]
    private partial void LogFileUnableToReadCreateDefaults(Exception ex, string fileName);

    [LoggerMessage(
        EventId = LogEventIDs.Infrastructure.SettingsProvider.FileInvalidOrEmpty,
        Level = LogLevel.Warning,
        Message = "File: {FileName} was empty or invalid. Reverting to factory defaults.")]
    private partial void LogFileInvalidOrEmptyCreateDefaults(string fileName);

    [LoggerMessage(
        EventId = LogEventIDs.Infrastructure.SettingsProvider.FileUnableToSave,
        Level = LogLevel.Warning,
        Message = "File: {FileName} - Unable to save.")]
    private partial void LogFileUnableToSave(Exception ex, string fileName);

    [LoggerMessage(
        EventId = LogEventIDs.Infrastructure.SettingsProvider.TempCleanupFailed,
        Level = LogLevel.Debug,
        Message = "Temp File: Could not be removed at {Path}.")]
    private partial void LogTempCleanupFailed(Exception ex, string path);
}