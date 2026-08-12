// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using OTCode.Core.Abstractions.Infrastructure;
using OTCode.Core.Configuration.AppSettings;
using OTCode.Core.Enums;
using OTCode.Infrastructure.Logging;
using OTCode.Infrastructure.Utils;

namespace OTCode.Infrastructure.Services;

public abstract partial class SettingsProvider<T> : ISettingsProvider<T>
{
    private readonly object _gate = new();
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = {new JsonStringEnumConverter()}
    };
    public T? Current {get; protected set;} = null!;

    // ─── PUBLIC METHODS ────────────────────────
    public virtual void Save()
    {
        lock(_gate)
            WriteToDisk(Current);
    }

    public virtual void Reload()
    {
        lock(_gate)
            ReloadCore();
    }

    // ─── PRIVATE METHODS ───────────────────────
    private void WriteToDisk(T settings)
    {
        var json = JsonSerializer.Serialize(settings, _jsonOptions);

        try
        {
            AtomicFileWriter.WriteTo(_settingsPath, json);
        }
        catch (Exception ex)
        {
            _logger.LogFileUnableToSave(ex, Path.GetFileName(_settingsPath));
        }
    }

    private void ReloadCore()
    {
        if (!File.Exists(_settingsPath))
        {
            _logger.LogFileNotFoundCreateDefaults(Path.GetFileName(_settingsPath));
            ApplyDefaults();
            return;
        }

        try
        {
            var json = File.ReadAllText(_settingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);

            if (settings is null)
            {
                _logger.LogFileInvalidOrEmptyCreateDefaults(Path.GetFileName(_settingsPath));
                ApplyDefaults();
                return;
            }

            Current = Sanitize(settings);
            Save();
        }
        catch (Exception ex)
        {
            _logger.LogFileUnableToReadCreateDefaults(ex, Path.GetFileName(_settingsPath));
            ApplyDefaults();
        }
    }

    private void ApplyDefaults()
    {
        Current = new T();
        Save();
    }

    protected abstract static T Sanitize(T settings);
}