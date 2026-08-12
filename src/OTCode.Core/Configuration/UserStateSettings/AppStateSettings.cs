// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace OTCode.Core.Configuration.UserStateSettings;

public sealed record AppStateSettings
{
    public string? LastOpenedDirectory {get; set;} = "";
}