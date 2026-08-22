// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using OTCode.Core.Enums;
using OTCode.Core.Extensions;

namespace OTCode.Core.Options.AvalonEdit;

public sealed class FontOptions
{
    public string Family {get; set;} = AppFont.Arial.ToExactString();
    public double Size {get; set;} = 12d;
}