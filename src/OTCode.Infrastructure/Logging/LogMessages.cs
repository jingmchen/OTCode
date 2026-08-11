// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Microsoft.Extensions.Logging;

namespace OTCode.Infrastructure.Logging;

internal static partial class LogMessages
{
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "No user appsettings file found - reverting to factory defaults.")]
    internal static partial void LogCreatingDefaults(this ILogger logger);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Warning,
        Message = "Unable to read appsettings file - reverting to factory defaults.")]
    internal static partial void LogUnableToLoad(this ILogger logger, Exception ex);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Warning,
        Message = "Appsettings file was empty or invalid - reverting to factory defaults.")]
    internal static partial void LogInvalidContent(this ILogger logger);

    [LoggerMessage(
        EventId = 1004,
        Level = LogLevel.Warning,
        Message = "Unable to persist appsettings file.")]
    internal static partial void LogUnableToSave(this ILogger logger, Exception ex);

    [LoggerMessage(
        EventId = 1005,
        Level = LogLevel.Debug,
        Message = "Could not remove temporary settings file '{Path}'; it will be overwritten on the next successful save.")]
    internal static partial void LogTempCleanupFailed(this ILogger logger, Exception ex, string path);
}