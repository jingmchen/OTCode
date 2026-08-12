// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Microsoft.Extensions.Logging;

namespace OTCode.Infrastructure.Logging;

internal static partial class LogMessages
{
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "File: '{FileName}' - Unable to be found. Reverting to factory defaults.")]
    internal static partial void LogFileNotFoundCreateDefaults(this ILogger logger, string fileName);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Warning,
        Message = "File: '{FileName}' - Unable to be read. Reverting to factory defaults.")]
    internal static partial void LogFileUnableToReadCreateDefaults(this ILogger logger, Exception ex, string fileName);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Warning,
        Message = "File: '{FileName}' was empty or invalid. Reverting to factory defaults.")]
    internal static partial void LogFileInvalidOrEmptyCreateDefaults(this ILogger logger, string fileName);

    [LoggerMessage(
        EventId = 1004,
        Level = LogLevel.Warning,
        Message = "File: '{FileName}' - Unable to save.")]
    internal static partial void LogFileUnableToSave(this ILogger logger, Exception ex, string fileName);

    [LoggerMessage(
        EventId = 1005,
        Level = LogLevel.Debug,
        Message = "Temp File: Could not be removed at '{Path}'; it may be overwritten on the next successful save.")]
    internal static partial void LogTempCleanupFailed(this ILogger logger, Exception ex, string path);
}