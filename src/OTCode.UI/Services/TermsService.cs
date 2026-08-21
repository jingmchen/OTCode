// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using Microsoft.Extensions.Logging;
using OTCode.Core.Abstractions.Infrastructure;
using OTCode.Core.Abstractions.UI;
using OTCode.Core.Configuration.UserStateSettings;
using OTCode.Core.Logging;
using OTCode.UI.Views.Dialogs;

namespace OTCode.UI.Services;

public sealed partial class TermsService : ITermsService
{
    private readonly ISettingsProvider<UserStateSettings> _settings;
    private readonly IResourceUriProvider _uri;
    private readonly ILogger<TermsService> _logger;
    
    public TermsService(
        ISettingsProvider<UserStateSettings> settings,
        IResourceUriProvider uri,
        ILogger<TermsService> logger)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _uri = uri ?? throw new ArgumentNullException(nameof(uri));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ─── PUBLIC METHODS ────────────────────────
    public bool CheckAcceptance()
    {
        var text = LoadBundledTermsConditions()
            ?? throw new InvalidDataException($"Bundled Terms Conditions invalid or not found.");

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

        if (string.Equals(_settings.Current.Terms.AcceptedTermsHash, hash, StringComparison.Ordinal))
            return true;
        
        var termsDialog = new TermsDialog(text);
        var accepted = termsDialog.ShowDialog();

        if (accepted != true)
        {
            LogTermsDeclined(hash);
            return false;
        }
        
        _settings.Current.Terms.AcceptedTermsHash = hash;
        _settings.Current.Terms.AcceptedAtUtc = DateTime.UtcNow;
        _settings.Current.Terms.AcceptedBy = Environment.UserName;

        try
        {
            _settings.Save();
        }
        catch (Exception ex)
        {
            LogUnableToPersistAcceptance(ex);
        }

        LogTermsAccepted(hash);
        return true;
    }

    // ─── PRIVATE METHODS ───────────────────────
    private string? LoadBundledTermsConditions()
    {
        try
        {
            var resource = Application.GetResourceStream(new Uri(_uri.TermsConditionsMarkdown, UriKind.Absolute));
            using var reader = new StreamReader(resource.Stream, Encoding.UTF8);
            return reader.ReadToEnd();
        }
        catch (Exception ex)
        {
            LogTermsUnavailable(ex, _uri.TermsConditionsMarkdown);
            return null;
        }
    }

    [LoggerMessage(
        EventId = LogEventIDs.UI.TermsService.TermsAccepted,
        Level = LogLevel.Information,
        Message = "Terms and Conditions (version {TermsHash}) accepted.")]
    private partial void LogTermsAccepted(string termsHash);

    [LoggerMessage(
        EventId = LogEventIDs.UI.TermsService.TermsDeclined,
        Level = LogLevel.Warning,
        Message = "Terms and Conditions (version {TermsHash}) declined — shutting down application.")]
    private partial void LogTermsDeclined(string termsHash);

    [LoggerMessage(
        EventId = LogEventIDs.UI.TermsService.TermsUnavailable,
        Level = LogLevel.Error,
        Message = "Bundled Terms and Conditions could not be loaded from {Uri} — shutting down application.")]
    private partial void LogTermsUnavailable(Exception ex, string uri);

    [LoggerMessage(
        EventId = LogEventIDs.UI.TermsService.UnableToPersistAcceptance,
        Level = LogLevel.Error,
        Message = "Could not persist the Terms and Conditions acceptance — the user will be asked again next launch.")]
    private partial void LogUnableToPersistAcceptance(Exception ex);
}