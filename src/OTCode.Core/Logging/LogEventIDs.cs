// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace OTCode.Core.Logging;

public static class LogEventIDs
{
    public static class Infrastructure
    {
        public static class SettingsProvider
        {
            public const int FileNotFound = 1001;
            public const int FileUnableToRead = 1002;
            public const int FileInvalidOrEmpty = 1003;
            public const int FileUnableToSave = 1004;
            public const int TempCleanupFailed = 1005;
        }
    }

    public static class UI
    {
        public static class TermsService
        {
            //
        }
    }
}