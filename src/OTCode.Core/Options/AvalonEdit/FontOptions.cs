// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Windows.UI.Text;
using OTCode.Core.Enums;

namespace OTCode.Core.Options.AvalonEdit;

public sealed record FontOptions
{
    public string Family {get; init;} = FontFamily.Arial.ToFontString();
    public double Size {get; init;} = 12;
    public FontWeight Weight {get; init;} = FontWeight.Normal;
}