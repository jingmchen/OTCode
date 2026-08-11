// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Performance", "CA1848",
    Justification = "Runs once at application startup.",
    Scope = "member",
    Target = "~M:OTCode.UI.Services.ThemeService.Initialize")]