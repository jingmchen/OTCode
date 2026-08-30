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
        }

        public static class FileWatcher
        {
            public const int FailedToStartMonitoring = 2201;
            public const int UnexpectedError = 2202;
        }
    }

    public static class UI
    {
        public static class TermsService
        {
            public const int TermsAccepted = 2001;
            public const int TermsDeclined = 2002;
            public const int TermsUnavailable = 2003;
            public const int UnableToPersistAcceptance = 2004;
        }

        public static class ThemeService
        {
            public const int AlreadyInitialized = 2101;
        }

        public static class FileExplorerViewModel
        {
            public const int FailedToLoadDirectory = 2201;
            public const int FailedToOpen = 2202;
            public const int FailedToShowProperties = 2203;
            public const int FailedToMoveOnDragDrop = 2204;
            public const int FailedToCreate = 2205;
            public const int FailedToRename = 2206;
            public const int FailedToPaste = 2207;
            public const int FailedToDelete = 2208;
            public const int FailedToDeleteMultiple = 2209;
            public const int FailedToRefreshDirectory = 2210;
        }
    }
}