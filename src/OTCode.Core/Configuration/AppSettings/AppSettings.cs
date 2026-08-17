// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace OTCode.Core.Configuration.AppSettings;

public sealed class AppSettings
{
    public ThemeSettings Theme {get; set;} = new();
    public LoggingSettings Logging {get; set;} = new();
}