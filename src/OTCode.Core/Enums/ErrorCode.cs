// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace OTCode.Core.Enums;

public enum ErrorCode
{
    None = 0,

    // 1XX - Bootstrap
    UnexpectedBootstrapError = 100,

    // 2XX - Configuration
    AppSettingsFileNotFound = 200,
    AppSettingsValidationFailed = 201,

    // 9XX - Internal
    NotImplemented = 900,
    UnexpectedError = 901
}