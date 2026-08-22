// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace OTCode.UI.Utils;

internal static class MarkdownRenderer
{
    private const double BodyFontSize = 13;
    private const double H1FontSize = 20;
    private const double H2FontSize = 16;
    private const double H3FontSize = 14;

    internal static StackPanel Render(string markdown)
    {
        ArgumentNullException.ThrowIfNull(markdown);

        var host = new StackPanel();
        var paragraph = new List<string>();

        foreach (var raw in markdown.Replace("\r\n", "\n").Split('\n'))
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
                
                host.Children.Add(new Separator
                {
                    Margin = new Thickness(0, 8, 0, 8)
                });

                continue;
            }

            if (trimmed.StartsWith('#'))
            {
                FlushParagraph(host, paragraph);
                host.Children.Add(CreateHeading(trimmed));
                continue;
            }

            if (trimmed.StartsWith("* ", StringComparison.Ordinal) ||
                trimmed.StartsWith("- ", StringComparison.Ordinal))
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

    internal static FlowDocument RenderDocument(string markdown)
    {
        ArgumentNullException.ThrowIfNull(markdown);

        var doc = new FlowDocument
        {
            AppFont = SystemFonts.MessageAppFont, // TODO swap in AppFont here
            FontSize = BodyFontSize,
            TextAlignment = TextAlignment.Left,
            PagePadding = new Thickness(14, 10, 14, 10),
            ColumnWidth = double.PositiveInfinity
        };

        var paragraph = new List<string>();
        List? bullets = null;

        foreach (var raw in markdown.Replace("\r\n", "\n").Split('\n'))
        {
            string line = raw.TrimEnd();
            string trimmed = line.TrimStart();

            if (trimmed.Length == 0)
            {
                FlushParagraph(doc, paragraph);
                bullets = null;
                continue;
            }

            if (trimmed is "---" or "***")
            {
                FlushParagraph(doc, paragraph);
                bullets = null;

                doc.Blocks.Add(new BlockUIContainer(new Separator())
                {
                    Margin = new Thickness(0, 8, 0, 8)
                });

                continue;
            }

            if (trimmed.StartsWith('#'))
            {
                FlushParagraph(doc, paragraph);
                bullets = null;
                doc.Blocks.Add(CreateDocHeading(trimmed));

                continue;
            }

            if (trimmed.StartsWith("* ", StringComparison.Ordinal) ||
                trimmed.StartsWith("- ", StringComparison.Ordinal))
            {
                FlushParagraph(doc, paragraph);

                if (bullets is null)
                {
                    bullets = new List
                    {
                        MarkerStyle = TextMarkerStyle.Disc,
                        Margin = new Thickness(10, 1, 0, 1),
                        Padding = new Thickness(18, 0, 0, 0),
                    };

                    doc.Blocks.Add(bullets);
                }

                var item = new Paragraph
                {
                    Margin = new Thickness(0, 1, 0, 1)
                };

                AppendInlines(item.Inlines, trimmed[2..]);
                bullets.ListItems.Add(new ListItem(item));

                continue;
            }

            bullets = null;
            paragraph.Add(trimmed);
        }
        FlushParagraph(doc, paragraph);
        return doc;
    }

    private static void FlushParagraph(StackPanel host, List<string> lines)
    {
        if (lines.Count == 0)
            return;
        
        var block = CreateBody(string.Join(' ', lines));
        block.Margin = new Thickness(0, 2, 0, 6);
        host.Children.Add(block);
        lines.Clear();
    }

    private static TextBlock CreateHeading(string line)
    {
        int level = HeadingLevel(line);
        var block = CreateBody(line[level..].TrimStart());

        block.FontSize = HeadingFontSize(level);
        block.FontWeight = level == 1 ? FontWeights.Bold : FontWeights.SemiBold;
        block.Margin = new Thickness(0, level == 1 ? 4 : 12, 0, 4);

        return block;
    }

    private static Grid CreateBullet(string text)
    {
        var grid = new Grid
        {
            Margin = new Thickness(10, 1, 0, 1),
        };

        grid.ColumnDefinitions.Add(new ColumnDefinition
        {
            Width = new GridLength(18)
        });

        grid.ColumnDefinitions.Add(new ColumnDefinition
        {
            Width = new GridLength(1, GridUnitType.Star)
        });

        var dot = new TextBlock
        {
            Text = "\u2022",
            FontSize = BodyFontSize,
            VerticalAlignment = VerticalAlignment.Top
        };

        var body = CreateBody(text);
        Grid.SetColumn(body, 1);
        grid.Children.Add(dot);
        grid.Children.Add(body);

        return grid;
    }

    private static TextBlock CreateBody(string text)
    {
        var block = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            FontSize = BodyFontSize,
        };

        AppendInlines(block.Inlines, text);
        return block;
    }

    private static void FlushParagraph(FlowDocument doc, List<string> lines)
    {
        if (lines.Count == 0)
            return;
        
        var p = new Paragraph
        {
            Margin = new Thickness(0, 2, 0, 6)
        };

        AppendInlines(p.Inlines, string.Join(' ', lines));
        doc.Blocks.Add(p);
        lines.Clear();
    }

    private static Paragraph CreateDocHeading(string line)
    {
        int level = HeadingLevel(line);

        var p = new Paragraph
        {
            FontSize = HeadingFontSize(level),
            FontWeight = level == 1 ? FontWeights.Bold : FontWeights.SemiBold,
            Margin = new Thickness(0, level == 1 ? 4 : 12, 0, 4),
        };

        AppendInlines(p.Inlines, line[level..].TrimStart());
        return p;
    }

    private static int HeadingLevel(string line)
    {
        int level = 0;

        while (level < line.Length && line[level] == '#')
            level++;
        
        return level;
    }

    private static void AppendInlines(InlineCollection inlines, string text)
    {
        var segments = text.Split("**");

        if (segments.Length == 1)
        {
            inlines.Add(new Run(text));
            return;
        }

        for (int i = 0; i < segments.Length; i++)
        {
            if (segments[i].Length == 0)
                continue;

            var run = new Run(segments[i]);

            if (i % 2 == 1)
                run.FontWeight = FontWeights.Bold;

            inlines.Add(run);
        }
    }

    private static double HeadingFontSize(int level) => level switch
    {
        1 => H1FontSize,
        2 => H2FontSize,
        _ => H3FontSize
    };
}