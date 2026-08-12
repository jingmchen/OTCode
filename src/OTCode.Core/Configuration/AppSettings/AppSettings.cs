// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace OTCode.Core.Configuration.AppSettings;

public sealed class AppSettings
{
    public ThemeSettings ThemeSection {get;} = new();
    public LoggingSettings LoggingSection {get;} = new();
    public TermsSettings TermsSection {get;} = new();
}