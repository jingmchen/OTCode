// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace OTCode.Core.Abstractions.Infrastructure;

public interface ISettingsProvider<T>
{
    T Current {get;}
    bool TrySave();
    void Reload();
}