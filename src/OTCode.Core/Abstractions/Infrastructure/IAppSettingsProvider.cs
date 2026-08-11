// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using OTCode.Core.Configuration;

namespace OTCode.Core.Abstractions.Infrastructure;

public interface IAppSettingsProvider
{
    AppSettings Current {get;}
    void Save();
    void Reload();
}