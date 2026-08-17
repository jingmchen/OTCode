// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace OTCode.Core.Configuration.UserStateSettings;

public sealed class UserStateSettings
{
    public AppStateSettings AppState {get; set;} = new();
    public TermsSettings Terms {get; set;} = new();
}