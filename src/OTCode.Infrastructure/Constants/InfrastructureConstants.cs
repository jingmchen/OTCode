// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace OTCode.Infrastructure.Constants;

internal static class InfrastructureConstants
{
    internal static class Bundled
    {
        internal static class FileName
        {
            internal const string AppSettings = "appsettings.json";
        }
    }

    internal static class UserData
    {
        internal static class FileName
        {
            internal const string AppSettings = "appsettings.json";
            internal const string UserStateSettings = "userstate.json";
            internal const string LatestLog = "latest.log";
            internal const string ArchivedLog = "archived.log";
        }

        internal static class FolderName
        {
            internal const string Logs = "logs";
            internal const string UserState = "state";
        }
    }

    internal static class Service
    {
        internal static class AppInfo
        {
            internal const string ProductDefault = "OTCode";
            internal const string CompanyDefault = "Tan Jing Ming";
            internal const string AuthorsDefault = "Tan Jing Ming";
            internal const string CopyrightDefault = $"Copyright (c) {CompanyDefault}. Use of this software is governed by LICENSE.md.";
        }
    }
}