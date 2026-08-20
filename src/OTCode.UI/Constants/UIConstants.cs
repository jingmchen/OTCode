// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace OTCode.UI.Constants;

internal static class UIConstants
{
    internal static class Bundled
    {
        internal static class FolderName
        {
            internal const string Assets = "Assets";
            internal const string Accents = "Accents";
            internal const string Icons = "Icons";
            internal const string Markdowns = "Markdowns";
            internal const string Themes = "Themes";
            internal const string Styles = "Styles";
        }

        internal static class FileName
        {
            internal const string TermsConditions = "TERMS_CONDITIONS.md";
        }
    }

    internal static class XAMLThemeKey
    {
        internal const string SystemAccentColor = "SystemAccentColor";
        internal const string AccentBrush = "AccentBrush";
    }

    internal static class Service
    {
        internal static class FileWatcher
        {
            internal const int DebounceMs = 400;
        }
    }

    internal static class Control
    {
        internal static class FileExplorer
        {
            internal const double DragThreshold = 4.0;
            internal const int AutoExpandDelayMs = 600;
            internal const double DefaultZoom = 13.0;
            internal const double MinZoom = 9.0;
            internal const double MaxZoom = 28.0;
            internal const double ZoomStep = 1.0;
        }
    }
}