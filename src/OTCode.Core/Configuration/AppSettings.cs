// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace OTCode.Core.Configuration;

public sealed class AppSettings
{
    public ThemeSettings ThemeSection {get; set;} = new();
    public LoggingSettings LoggingSection {get; set;} = new();
    public TermsSettings TermsSection {get; set;} = new();
}