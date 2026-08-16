// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Media;
using Windows.UI.ViewManagement;
using Microsoft.Extensions.Logging;
using OTCode.Core.Abstractions.Infrastructure;
using OTCode.Core.Abstractions.UI;
using OTCode.Core.Configuration.AppSettings;
using OTCode.Core.Enums;
using OTCode.UI.Constants;
using OTCode.UI.Utils;

namespace OTCode.UI.Services;

public sealed class ThemeService : IThemeService
{
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
    private bool _isDisposed;
    public AppTheme CurrentTheme {get; private set;}
    public AppAccent CurrentAccent {get; private set;}
    public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;
    
    public ThemeService(ISettingsProvider<AppSettings> settings, ILogger<ThemeService> logger, IUriPaths uriPaths)
    {
        ArgumentNullException.ThrowIfNull(uriPaths);
        
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _themeTemplate = CompositeFormat.Parse(uriPaths.ThemeTemplate);
        _accentTemplate = CompositeFormat.Parse(uriPaths.AccentTemplate);

        Initialize();
    }

    // ─── PUBLIC METHODS ────────────────────────
    public void Initialize()
    {
        if (_isInitialized)
        {
            _logger.LogWarning("{ThemeService} is already initialized.", nameof(ThemeService));
            return;
        }

        ThrowIfAppNotReady();

        CurrentTheme = _settings.Current.ThemeSection.Theme;
        CurrentAccent = _settings.Current.ThemeSection.Accent;

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

        DispatcherHelper.PostOnUIThread(() => ApplyCore(theme, accent, fireEvent: true, persist: true));
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;
        _isDisposed = true;
        _uiSettings.ColorValuesChanged -= OnSystemThemeChanged;
    }

    // ─── PRIVATE METHODS ───────────────────────
    private void ApplyCore(AppTheme theme, AppAccent accent, bool fireEvent, bool persist = true)
    {
        if (_isDisposed)
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
        
        // Call EditorSyntax to update palette - keep this here, I will implement AvalonEditor later
        // EditorSyntax.SetTheme(isDark, GetColor(accentDictionary));

        if (persist)
            Persist();

        if (fireEvent)
            ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(theme, accent));
    }

    private void Persist()
    {
        _settings.Current.ThemeSection.Theme = CurrentTheme;
        _settings.Current.ThemeSection.Accent = CurrentAccent;
        _settings.Save();
    }

    private void OnSystemThemeChanged(UISettings sender, object args)
    {
        if (_isDisposed || CurrentTheme != AppTheme.System)
            return;

        DispatcherHelper.PostOnUIThread(
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
    
    // Color helpers for AvalonEditor
    private Color GetColor(ResourceDictionary dictionary)
    {
        if (dictionary.Contains(UIConstants.XAMLThemeKeys.SystemAccentColor)
            && dictionary[UIConstants.XAMLThemeKeys.SystemAccentColor] is Color color)
                return color;
        
        if (dictionary.Contains(UIConstants.XAMLThemeKeys.AccentBrush)
            && dictionary[UIConstants.XAMLThemeKeys.AccentBrush] is SolidColorBrush brush)
                return brush.Color;

        throw new KeyNotFoundException($"Accent keys not found in {CurrentAccent}.");
    }

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
        => ObjectDisposedException.ThrowIf(_isDisposed, nameof(ThemeService));
}