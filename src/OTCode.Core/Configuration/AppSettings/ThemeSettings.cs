// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using OTCode.Core.Enums;

namespace OTCode.Core.Configuration.AppSettings;

public sealed class ThemeSettings
{
    public AppTheme Theme {get; set;} = AppTheme.Light;
    public AppAccent Accent {get; set;} = AppAccent.Black;
}