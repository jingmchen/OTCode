// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using DocumentList = System.Windows.Documents.List;
using OTCode.Core.Enums;

namespace OTCode.UI.Utils;

internal static class MarkdownRenderer
{
    private const double BodyFontSize = 14;

    // Fall backs when corresponding application style is not found
    private const double UiH1FontSize = 32;
    private const double UiH2FontSize = 24;
    private const double UiH3FontSize = 18;
    private const double DocumentH1FontSize = 28;
    private const double DocumentH2FontSize = 22;
    private const double DocumentH3FontSize = 17;

    private const string DefaultTextBlockStyleKey = "DefaultTextBlockStyle";
    private const string DefaultFlowDocumentStyleKey = "DefaultFlowDocumentStyle";
    private const string DocumentParagraphStyleKey = "DocumentParagraphStyle";
    private const string DocumentHeading1StyleKey = "DocumentHeading1Style";
    private const string DocumentHeading2StyleKey = "DocumentHeading2Style";
    private const string DocumentHeading3StyleKey = "DocumentHeading3Style";
    private const string ThemeForegroundBrushKey = "ThemeFg0Brush";
    private const string ThemeBorderBrushKey = "ThemeBorderBrush";

    internal static StackPanel Render(
        string markdown,
        AppFont font = AppFont.SegoeUIVariable)
    {
        ArgumentNullException.ThrowIfNull(markdown);

        var host = new StackPanel();
        
        host.SetResourceReference(TextElement.FontFamilyProperty, font);
        host.SetValue(TextElement.FontSizeProperty, BodyFontSize);

        var paragraph = new List<string>();

        foreach (string raw in NormalizeLines(markdown))
        {
            string line = raw.TrimEnd();
            string trimmed = line.TrimStart();

            if (trimmed.Length == 0)
            {
                FlushParagraph(host, paragraph);
                continue;
            }

            if (trimmed is "---" or "***")
            {
                FlushParagraph(host, paragraph);
                host.Children.Add(CreateRule());
                continue;
            }

            if (trimmed.StartsWith('#'))
            {
                FlushParagraph(host, paragraph);
                host.Children.Add(CreateHeading(trimmed));
                continue;
            }

            if (IsBullet(trimmed))
            {
                FlushParagraph(host, paragraph);
                host.Children.Add(CreateBullet(trimmed[2..]));
                continue;
            }

            paragraph.Add(trimmed);
        }

        FlushParagraph(host, paragraph);
        return host;
    }

    internal static FlowDocument RenderDocument(
        string markdown,
        AppFont font = AppFont.SegoeUIVariable)
    {
        ArgumentNullException.ThrowIfNull(markdown);

        var document = new FlowDocument
        {
            ColumnWidth = double.PositiveInfinity
        };

        if (!TryApplyStyle(document, DefaultFlowDocumentStyleKey))
        {
            document.FontSize = BodyFontSize;
            document.TextAlignment = TextAlignment.Left;
            document.PagePadding = new Thickness(14, 10, 14, 10);
        }

        // DynamicResource semantics for theme and font resources
        document.SetResourceReference(TextElement.FontFamilyProperty, font);
        document.SetResourceReference(
            TextElement.ForegroundProperty,
            ThemeForegroundBrushKey);

        var paragraph = new List<string>();
        DocumentList? bullets = null;

        foreach (string raw in NormalizeLines(markdown))
        {
            string line = raw.TrimEnd();
            string trimmed = line.TrimStart();

            if (trimmed.Length == 0)
            {
                FlushParagraph(document, paragraph);
                bullets = null;
                continue;
            }

            if (trimmed is "---" or "***")
            {
                FlushParagraph(document, paragraph);
                bullets = null;

                document.Blocks.Add(new BlockUIContainer(CreateRule()));
                continue;
            }

            if (trimmed.StartsWith('#'))
            {
                FlushParagraph(document, paragraph);
                bullets = null;
                document.Blocks.Add(CreateDocumentHeading(trimmed));
                continue;
            }

            if (IsBullet(trimmed))
            {
                FlushParagraph(document, paragraph);

                if (bullets is null)
                {
                    bullets = new DocumentList
                    {
                        MarkerStyle = TextMarkerStyle.Disc,
                        Margin = new Thickness(10, 1, 0, 1),
                        Padding = new Thickness(18, 0, 0, 0)
                    };

                    document.Blocks.Add(bullets);
                }

                var itemParagraph = new Paragraph
                {
                    Margin = new Thickness(0, 1, 0, 1)
                };

                AppendInlines(itemParagraph.Inlines, trimmed[2..]);
                bullets.ListItems.Add(new ListItem(itemParagraph));
                continue;
            }

            bullets = null;
            paragraph.Add(trimmed);
        }

        FlushParagraph(document, paragraph);
        return document;
    }

    private static void FlushParagraph(
        StackPanel host,
        List<string> lines)
    {
        if (lines.Count == 0)
            return;

        TextBlock block = CreateBody(string.Join(' ', lines));
        block.Margin = new Thickness(0, 2, 0, 6);

        host.Children.Add(block);
        lines.Clear();
    }

    private static TextBlock CreateHeading(string line)
    {
        int level = HeadingLevel(line);
        var block = CreateBody(line[level..].TrimStart());

        string styleKey = level switch
        {
            1 => "h1",
            2 => "h2",
            _ => "h3"
        };

        if (!TryApplyStyle(block, styleKey))
        {
            block.FontSize = UiHeadingFontSize(level);
            block.FontWeight = level == 1
                ? FontWeights.Bold
                : FontWeights.SemiBold;
        }

        block.Margin = new Thickness(
            0,
            level == 1 ? 4 : 12,
            0,
            4);

        return block;
    }

    private static Grid CreateBullet(string text)
    {
        var grid = new Grid
        {
            Margin = new Thickness(10, 1, 0, 1)
        };

        grid.ColumnDefinitions.Add(new ColumnDefinition
        {
            Width = new GridLength(18)
        });

        grid.ColumnDefinitions.Add(new ColumnDefinition
        {
            Width = new GridLength(1, GridUnitType.Star)
        });

        var marker = new TextBlock
        {
            Text = "\u2022",
            FontSize = BodyFontSize,
            VerticalAlignment = VerticalAlignment.Top
        };

        TryApplyStyle(marker, DefaultTextBlockStyleKey);

        TextBlock body = CreateBody(text);
        Grid.SetColumn(body, 1);

        grid.Children.Add(marker);
        grid.Children.Add(body);

        return grid;
    }

    private static TextBlock CreateBody(string text)
    {
        var block = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap
        };

        if (!TryApplyStyle(block, DefaultTextBlockStyleKey))
            block.FontSize = BodyFontSize;

        AppendInlines(block.Inlines, text);
        return block;
    }

    private static void FlushParagraph(
        FlowDocument document,
        List<string> lines)
    {
        if (lines.Count == 0)
            return;

        var paragraph = new Paragraph();

        if (!TryApplyStyle(paragraph, DocumentParagraphStyleKey))
        {
            paragraph.Margin = new Thickness(0, 2, 0, 6);
            paragraph.LineHeight = 21;
        }

        AppendInlines(paragraph.Inlines, string.Join(' ', lines));
        document.Blocks.Add(paragraph);
        lines.Clear();
    }

    private static Paragraph CreateDocumentHeading(string line)
    {
        int level = HeadingLevel(line);
        var paragraph = new Paragraph();

        string styleKey = level switch
        {
            1 => DocumentHeading1StyleKey,
            2 => DocumentHeading2StyleKey,
            _ => DocumentHeading3StyleKey
        };

        if (!TryApplyStyle(paragraph, styleKey))
        {
            paragraph.FontSize = DocumentHeadingFontSize(level);
            paragraph.FontWeight = level == 1
                ? FontWeights.Bold
                : FontWeights.SemiBold;
            paragraph.Margin = new Thickness(
                0,
                level == 1 ? 4 : 12,
                0,
                4);
        }

        AppendInlines(paragraph.Inlines, line[level..].TrimStart());
        return paragraph;
    }

    private static Border CreateRule()
    {
        var rule = new Border
        {
            Height = 1,
            Margin = new Thickness(0, 8, 0, 8)
        };

        rule.SetResourceReference(
            Border.BackgroundProperty,
            ThemeBorderBrushKey);

        return rule;
    }

    private static int HeadingLevel(string line)
    {
        int level = 0;

        while (level < line.Length && line[level] == '#')
            level++;

        return level;
    }

    private static void AppendInlines(
        InlineCollection inlines,
        string text)
    {
        string[] segments = text.Split("**");

        if (segments.Length == 1)
        {
            inlines.Add(new Run(text));
            return;
        }

        for (int index = 0; index < segments.Length; index++)
        {
            if (segments[index].Length == 0)
                continue;

            var run = new Run(segments[index]);

            if (index % 2 == 1)
                run.FontWeight = FontWeights.Bold;

            inlines.Add(run);
        }
    }

    private static bool TryApplyStyle(
        FrameworkElement element,
        object resourceKey)
    {
        if (Application.Current?.TryFindResource(resourceKey) is not Style style)
            return false;

        element.Style = style;
        return true;
    }

    private static bool TryApplyStyle(
        FrameworkContentElement element,
        object resourceKey)
    {
        if (Application.Current?.TryFindResource(resourceKey) is not Style style)
            return false;

        element.Style = style;
        return true;
    }

    private static string[] NormalizeLines(string markdown) =>
        markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');

    private static bool IsBullet(string line) =>
        line.StartsWith("* ", StringComparison.Ordinal) ||
        line.StartsWith("- ", StringComparison.Ordinal);

    private static double UiHeadingFontSize(int level) => level switch
    {
        1 => UiH1FontSize,
        2 => UiH2FontSize,
        _ => UiH3FontSize
    };

    private static double DocumentHeadingFontSize(int level) => level switch
    {
        1 => DocumentH1FontSize,
        2 => DocumentH2FontSize,
        _ => DocumentH3FontSize
    };
}