// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace OTCode.Core.Abstractions.UI;

public interface IUIDispatcher
{
    void Post(Action action);
}