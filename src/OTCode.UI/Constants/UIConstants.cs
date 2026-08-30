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
            internal const string Styles = "Styles";
            internal const string Themes = "Themes";
        }

        internal static class FileName
        {
            internal const string AccentTemplate = "{0}Accent.xaml";
            internal const string IconTemplate = "{0}.png";
            internal const string MarkdownTemplate = "{0}.md";
            internal const string StyleTemplate = "{0}.xaml";
            internal const string ThemeTemplate = "{0}Theme.xaml";
            internal const string TermsConditionsMarkdown = "TERMS_CONDITIONS.md";
        }
    }

    internal static class XAMLKeys
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

        internal static class AvalonEditService
        {
            /// <summary> Smallest permitted editor font size </summary>
            internal const double MinFontSize = 6d;

            /// <summary> Largest permitted editor font size </summary>
            internal const double MaxFontSize = 72d;

            /// <summary> Font-size change applied per zoom step </summary>
            internal const double Step = 1d;
        }
    }

    internal static class Control
    {
        internal static class FileExplorer
        {
            internal const int AutoExpandDelayMs = 600;
            internal const double DefaultZoom = 13.0;
            internal const double MinZoom = 9.0;
            internal const double MaxZoom = 28.0;
            internal const double ZoomStep = 1.0;
        }
    }

    internal static class Behavior
    {
        internal static class DragSource
        {
            internal const double DragThreshold = 4.0;
        }
    }
}