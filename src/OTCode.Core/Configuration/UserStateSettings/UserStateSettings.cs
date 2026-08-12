// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace OTCode.Core.Configuration.UserStateSettings;

public sealed class UserStateSettings
{
    public AppStateSettings AppStateSection {get;} = new();
    public TermsSettings TermsSection {get;} = new();
}