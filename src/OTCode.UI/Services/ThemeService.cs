// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Globalization;
using System.Text;
using System.Windows;
using Windows.UI.ViewManagement;
using Microsoft.Extensions.Logging;
using OTCode.Core.Abstractions.Infrastructure;
using OTCode.Core.Abstractions.UI;
using OTCode.Core.Configuration.AppSettings;
using OTCode.Core.Enums;
using OTCode.Core.Logging;

namespace OTCode.UI.Services;

public sealed partial class ThemeService : IThemeService
{
    private readonly IUIDispatcher _dispatcher;
    private readonly ISettingsProvider<AppSettings> _settings;
    private readonly ILogger<ThemeService> _logger;
    private ResourceDictionary? _themeSlot;
    private ResourceDictionary? _accentSlot;
    private readonly Dictionary<AppTheme, ResourceDictionary> _themeCache = [];
    private readonly Dictionary<AppAccent, ResourceDictionary> _accentCache = [];
    private readonly UISettings _uiSettings = new();
    private readonly CompositeFormat _themeTemplate;
    private readonly CompositeFormat _accentTemplate;
    private bool _isInitialized;
    private bool _disposed;
    public AppTheme CurrentTheme {get; private set;}
    public AppAccent CurrentAccent {get; private set;}
    public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;
    
    public ThemeService(
        IUIDispatcher dispatcher,
        ISettingsProvider<AppSettings> settings,
        ILogger<ThemeService> logger,
        IResourceUriProvider uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _themeTemplate = CompositeFormat.Parse(uri.ThemeTemplate);
        _accentTemplate = CompositeFormat.Parse(uri.AccentTemplate);
    }

    // ─── PUBLIC METHODS ────────────────────────
    public void Initialize()
    {
        if (_isInitialized)
        {
            LogAlreadyInitialized();
            return;
        }

        ThrowIfAppNotReady();

        CurrentTheme = _settings.Current.Theme.Theme;
        CurrentAccent = _settings.Current.Theme.Accent;

        _themeSlot = new ResourceDictionary();
        _accentSlot = new ResourceDictionary();

        var merged = Application.Current!.Resources.MergedDictionaries;

        merged.Add(_themeSlot);
        merged.Add(_accentSlot);

        ApplyCore(CurrentTheme, CurrentAccent, fireEvent: false, persist: true);

        // Subscribe to OS theme changes
        _uiSettings.ColorValuesChanged += OnSystemThemeChanged;

        _isInitialized = true;
    }

    public void SetTheme(AppTheme theme)
        => SetBoth(theme, CurrentAccent);
    
    public void SetAccent(AppAccent accent)
        => SetBoth(CurrentTheme, accent);
    
    public void SetBoth(AppTheme theme, AppAccent accent)
    {
        ThrowIfNotInitialized();
        ThrowIfDisposed();

        if (theme == CurrentTheme && accent == CurrentAccent)
            return;

        _dispatcher.Post(() => ApplyCore(theme, accent, fireEvent: true, persist: true));
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _uiSettings.ColorValuesChanged -= OnSystemThemeChanged;
    }

    // ─── PRIVATE METHODS ───────────────────────
    private void ApplyCore(AppTheme theme, AppAccent accent, bool fireEvent, bool persist = true)
    {
        if (_disposed)
            return;

        CurrentTheme = theme;
        CurrentAccent = accent;
        
        var effectiveTheme = theme == AppTheme.System ? GetSystemTheme() : theme;
        var isDark = IsDarkTheme(effectiveTheme);

        // Fluent theme for built-in control styles
        Application.Current!.ThemeMode =
            isDark ? ThemeMode.Dark : ThemeMode.Light;

        var merged = Application.Current!.Resources.MergedDictionaries;
        var themeDictionary = GetOrLoadDictionary(_themeCache, effectiveTheme, ThemeUri);
        var accentDictionary = GetOrLoadDictionary(_accentCache, CurrentAccent, AccentUri);

        _themeSlot?.MergedDictionaries.Clear();
        _themeSlot?.MergedDictionaries.Add(themeDictionary);

        _accentSlot?.MergedDictionaries.Clear();
        _accentSlot?.MergedDictionaries.Add(accentDictionary);

        if (persist)
            Persist();

        if (fireEvent)
            ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(theme, accent));
    }

    private void Persist()
    {
        _settings.Current.Theme.Theme = CurrentTheme;
        _settings.Current.Theme.Accent = CurrentAccent;
        _settings.TrySave();
    }

    private void OnSystemThemeChanged(UISettings sender, object args)
    {
        if (_disposed || CurrentTheme != AppTheme.System)
            return;

        _dispatcher.Post(
            () => ApplyCore(AppTheme.System, CurrentAccent, fireEvent: true, persist: true));
    }
    
    private AppTheme GetSystemTheme()
        => IsColorLight(_uiSettings.GetColorValue(UIColorType.Background))
            ? AppTheme.Black
            : AppTheme.Light;

    private static bool IsDarkTheme(AppTheme theme)
        => theme switch
        {
            AppTheme.Black or AppTheme.DarkGraphite or AppTheme.DarkNavy => true,
            AppTheme.Light or AppTheme.White => false,
            _ => throw new ArgumentOutOfRangeException(nameof(theme), theme, "Invalid theme.")
        };
    
    private static bool IsColorLight(Windows.UI.Color c)
        => (5 * c.G + 2 * c.R + c.B) > 8 * 128;

    // Cache helpers
    private static ResourceDictionary GetOrLoadDictionary<TKey>(
        Dictionary<TKey, ResourceDictionary> cache,
        TKey key,
        Func<TKey, Uri> uriFactory) where TKey : notnull
    {
        if (!cache.TryGetValue(key, out var dict))
        {
            dict = LoadDictionary(uriFactory(key));
            cache[key] = dict;
        }
        return dict;
    }

    private static ResourceDictionary LoadDictionary(Uri uri)
        => new() {Source = uri};

    // Uri helpers
    private Uri ThemeUri(AppTheme theme)
        => new(string.Format(CultureInfo.InvariantCulture, _themeTemplate, theme));
    
    private Uri AccentUri(AppAccent accent)
        => new(string.Format(CultureInfo.InvariantCulture, _accentTemplate, accent));
    

    // Guard
    private void ThrowIfNotInitialized()
    {
        if (!_isInitialized)
            throw new InvalidOperationException($"{nameof(ThemeService)} is not initialized yet.");
    }

    private static void ThrowIfAppNotReady()
    {
        if (Application.Current is null)
            throw new InvalidOperationException("Application is not yet ready.");
    }

    private void ThrowIfDisposed()
        => ObjectDisposedException.ThrowIf(_disposed, nameof(ThemeService));
    
    [LoggerMessage(
        EventId = LogEventIDs.UI.ThemeService.AlreadyInitialized,
        Level = LogLevel.Warning,
        Message = "Service is already initialized.")]
    private partial void LogAlreadyInitialized();
}