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
            internal const int InternalBufferSize = 65_536;
        }
    }
}