// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using OTCode.Core.Configuration.UserStateSettings;

namespace OTCode.Infrastructure.Services;

public sealed class UserStateSettingsProvider : ISettingsProvider<UserStateSettings>
{
    private readonly ILogger<UserStateSettingsProvider> _logger;
    private readonly object _gate = new();
    private readonly string _settingsPath;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };
    public UserStateSettings Current {get; private set;} = null!;

    public UserStateSettingsProvider(ILogger<UserStateSettingsProvider> logger, IAppPaths appPaths)
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
    private void WriteToDisk(UserStateSettings settings)
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
}