// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using OTCode.Core.Enums;

namespace OTCode.Core.Extensions;

public static class AppFontExtension
{
    public static string ToExactString(this AppFont font) => font switch
    {
        AppFont.SegoeUI => "Segoe UI",
        AppFont.SegoeUIVariable => "Segoe UI Variable",
        AppFont.Arial => "Arial",
        AppFont.Calibri => "Calibri",
        AppFont.Tahoma => "Tahoma",
        AppFont.Verdana => "Verdana",
        AppFont.CascadiaCode => "Cascadia Code",
        AppFont.CascadiaMono => "Cascadia Mono",
        AppFont.Consolas => "Consolas",
        AppFont.CourierNew => "Courier New",
        AppFont.LucidaConsole => "Lucida Console",
        AppFont.DejaVuSansMono => "DejaVu Sans Mono",
        AppFont.SourceCodePro => "Source Code Pro",
        AppFont.JetBrainsMono => "JetBrains Mono",
        AppFont.FiraCode => "Fira Code",
        AppFont.IBMPlexMono => "IBM Plex Mono",
        AppFont.RobotoMono => "Roboto Mono",
        _ => throw new ArgumentOutOfRangeException(nameof(font), font, "Unsupported Font Family")
    };
}