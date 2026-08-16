// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Microsoft.Extensions.Logging;
using OTCode.Core.Abstractions.Infrastructure;
using OTCode.Core.Abstractions.UI;
using OTCode.Core.Configuration.UserStateSettings;

namespace OTCode.UI.Services;

public sealed class TermsService : ITermsService
{
    private readonly ISettingsProvider<UserStateSettings> _settings;
    private readonly IUriPaths _paths;
    private readonly ILogger<TermsService> _logger;
    public bool IsAcceptanceRequired {get;}
    
    public TermsService(ISettingsProvider<UserStateSettings> settings, IUriPaths paths, ILogger<TermsService> logger)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> EnsureAcceptedAsync()
    {
        //
    }
}