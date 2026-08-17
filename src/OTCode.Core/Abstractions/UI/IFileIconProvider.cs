// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace OTCode.Core.Abstractions.UI;

public interface IFileIconProvider
{
    string GetIcon(
        string name, string extension, bool isDirectory, bool isExpanded
    );
}