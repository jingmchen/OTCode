// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace OTCode.Core.Options.AvalonEdit;

public sealed record ColorOptions
{
    public string? ForegroundColor {get; init;}
    public string? BackgroundColor {get; init;}
    public string? LineNumbersColor {get; init;}
    public string? CurrentLineColor {get; init;}
    public string? SelectionColor {get; init;}
}