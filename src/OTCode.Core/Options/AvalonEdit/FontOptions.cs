// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using OTCode.Core.Enums;

namespace OTCode.Core.Options.AvalonEdit;

public sealed record FontOptions
{
    public FontFamily Family {get; init;}

    private string GetFamilyName() => Family switch
    {
        FontFamily.SegoeUI => "Segoe UI",
        FontFamily.SegoeUIVariable => "Segoe UI Variable",
        FontFamily.Arial => "Arial",
        FontFamily.Calibri => "Calibri",
        FontFamily.Tahoma => "Tahoma",
        FontFamily.Verdana => "Verdana",
        FontFamily.CascadiaCode => "Cascadia Code",
        FontFamily.CascadiaMono => "Cascadia Mono",
        FontFamily.Consolas => "Consolas",
        FontFamily.CourierNew => "Courier New",
        FontFamily.LucidaConsole => "Lucida Console",
        FontFamily.DejaVuSansMono => "Deja Vu Sans Mono",
        FontFamily.SourceCodePro => "Source Code Pro",
        FontFamily.JetBrainsMono => "Jet Brains Mono",
        FontFamily.FiraCode => "Fira Code",
        FontFamily.IBMPlexMono => "IBM Plex Mono",
        FontFamily.RobotoMono => "Roboto Mono",
        _ => throw new ArgumentOutOfRangeException(nameof(Family), Family, "Unsupported Font Family")
    };
}