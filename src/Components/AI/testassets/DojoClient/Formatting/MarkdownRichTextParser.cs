// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.AI;

namespace DojoClient.Formatting;

internal static class MarkdownRichTextParser
{
    internal static IReadOnlyList<RichTextNode> Parse(string markdown)
    {
        ArgumentNullException.ThrowIfNull(markdown);

        var lines = markdown.ReplaceLineEndings("\n").Split('\n');
        var nodes = new List<RichTextNode>();
        var lineIndex = 0;
        while (lineIndex < lines.Length)
        {
            if (string.IsNullOrWhiteSpace(lines[lineIndex]))
            {
                lineIndex++;
                continue;
            }

            if (TryParseCodeBlock(lines, ref lineIndex, out var block))
            {
                nodes.Add(block);
                continue;
            }
            if (TryParseHeading(lines[lineIndex], out block) ||
                TryParseThematicBreak(lines[lineIndex], out block))
            {
                nodes.Add(block);
                lineIndex++;
                continue;
            }
            if (TryParseBlockQuote(lines, ref lineIndex, out block) ||
                TryParseList(lines, ref lineIndex, out block))
            {
                nodes.Add(block);
                continue;
            }

            var paragraphLines = new List<string>();
            while (lineIndex < lines.Length &&
                !string.IsNullOrWhiteSpace(lines[lineIndex]) &&
                !StartsBlock(lines, lineIndex))
            {
                paragraphLines.Add(lines[lineIndex]);
                lineIndex++;
            }

            if (paragraphLines.Count == 0)
            {
                paragraphLines.Add(lines[lineIndex]);
                lineIndex++;
            }

            var paragraph = new ParagraphNode();
            for (var i = 0; i < paragraphLines.Count; i++)
            {
                AddInlineNodes(paragraph, paragraphLines[i]);
                if (i < paragraphLines.Count - 1)
                {
                    paragraph.AddChild(new LineBreakNode());
                }
            }
            nodes.Add(paragraph);
        }

        return nodes;
    }

    private static bool TryParseCodeBlock(
        string[] lines,
        ref int lineIndex,
        out RichTextNode node)
    {
        var line = lines[lineIndex];
        if (!line.StartsWith("```", StringComparison.Ordinal))
        {
            node = null!;
            return false;
        }

        var language = line[3..].Trim();
        var codeLines = new List<string>();
        lineIndex++;
        while (lineIndex < lines.Length &&
            !lines[lineIndex].StartsWith("```", StringComparison.Ordinal))
        {
            codeLines.Add(lines[lineIndex]);
            lineIndex++;
        }
        if (lineIndex < lines.Length)
        {
            lineIndex++;
        }

        node = new CodeBlockNode(
            string.Join('\n', codeLines),
            language.Length == 0 ? null : language);
        return true;
    }

    private static bool TryParseHeading(string line, out RichTextNode node)
    {
        var level = 0;
        while (level < line.Length && level < 6 && line[level] == '#')
        {
            level++;
        }

        if (level == 0 || level >= line.Length || line[level] != ' ')
        {
            node = null!;
            return false;
        }

        var heading = new HeadingNode(level);
        AddInlineNodes(heading, line[(level + 1)..]);
        node = heading;
        return true;
    }

    private static bool TryParseThematicBreak(string line, out RichTextNode node)
    {
        var value = line.Trim();
        if (value is "---" or "***" or "___")
        {
            node = new ThematicBreakNode();
            return true;
        }

        node = null!;
        return false;
    }

    private static bool TryParseBlockQuote(
        string[] lines,
        ref int lineIndex,
        out RichTextNode node)
    {
        if (!lines[lineIndex].StartsWith('>'))
        {
            node = null!;
            return false;
        }

        var quotedLines = new List<string>();
        while (lineIndex < lines.Length && lines[lineIndex].StartsWith('>'))
        {
            var line = lines[lineIndex][1..];
            quotedLines.Add(line.StartsWith(' ') ? line[1..] : line);
            lineIndex++;
        }

        var quote = new BlockQuoteNode();
        foreach (var child in Parse(string.Join('\n', quotedLines)))
        {
            quote.AddChild(child);
        }
        node = quote;
        return true;
    }

    private static bool TryParseList(
        string[] lines,
        ref int lineIndex,
        out RichTextNode node)
    {
        if (!TryGetListItem(lines[lineIndex], out var ordered, out var start, out _))
        {
            node = null!;
            return false;
        }

        var list = new ListNode(ordered, start);
        while (lineIndex < lines.Length &&
            TryGetListItem(lines[lineIndex], out var itemOrdered, out _, out var itemText) &&
            itemOrdered == ordered)
        {
            var item = new ListItemNode();
            if (itemText.StartsWith("[ ] ", StringComparison.Ordinal))
            {
                item.Checked = false;
                itemText = itemText[4..];
            }
            else if (itemText.StartsWith("[x] ", StringComparison.OrdinalIgnoreCase))
            {
                item.Checked = true;
                itemText = itemText[4..];
            }

            var paragraph = new ParagraphNode();
            AddInlineNodes(paragraph, itemText);
            item.AddChild(paragraph);
            list.AddChild(item);
            lineIndex++;
        }

        node = list;
        return true;
    }

    private static bool TryGetListItem(
        string line,
        out bool ordered,
        out int? start,
        out string item)
    {
        if (line.StartsWith("- ", StringComparison.Ordinal) ||
            line.StartsWith("* ", StringComparison.Ordinal))
        {
            ordered = false;
            start = null;
            item = line[2..];
            return true;
        }

        var markerEnd = line.IndexOf(". ", StringComparison.Ordinal);
        if (markerEnd > 0 && int.TryParse(line.AsSpan(0, markerEnd), out var number))
        {
            ordered = true;
            start = number;
            item = line[(markerEnd + 2)..];
            return true;
        }

        ordered = false;
        start = null;
        item = string.Empty;
        return false;
    }

    private static bool StartsBlock(string[] lines, int lineIndex)
    {
        var line = lines[lineIndex];
        return line.StartsWith("```", StringComparison.Ordinal) ||
            line.StartsWith('>') ||
            TryParseHeading(line, out _) ||
            TryParseThematicBreak(line, out _) ||
            TryGetListItem(line, out _, out _, out _);
    }

    private static void AddInlineNodes(RichTextNode parent, string text)
    {
        var position = 0;
        while (position < text.Length)
        {
            if (TryAddImage(parent, text, ref position) ||
                TryAddLink(parent, text, ref position) ||
                TryAddDelimited(parent, text, ref position, "**", static () => new StrongNode()) ||
                TryAddDelimited(parent, text, ref position, "~~", static () => new StrikethroughNode()) ||
                TryAddDelimited(parent, text, ref position, "*", static () => new EmphasisNode()) ||
                TryAddInlineCode(parent, text, ref position))
            {
                continue;
            }

            var nextMarker = FindNextMarker(text, position + 1);
            var length = nextMarker < 0 ? text.Length - position : nextMarker - position;
            parent.AddChild(new TextNode(text.Substring(position, length)));
            position += length;
        }
    }

    private static bool TryAddImage(RichTextNode parent, string text, ref int position)
    {
        if (!text.AsSpan(position).StartsWith("![", StringComparison.Ordinal))
        {
            return false;
        }

        var labelEnd = text.IndexOf("](", position + 2, StringComparison.Ordinal);
        var targetEnd = labelEnd < 0 ? -1 : text.IndexOf(')', labelEnd + 2);
        if (labelEnd < 0 || targetEnd < 0)
        {
            return false;
        }

        parent.AddChild(new ImageNode(
            text[(labelEnd + 2)..targetEnd],
            text[(position + 2)..labelEnd]));
        position = targetEnd + 1;
        return true;
    }

    private static bool TryAddLink(RichTextNode parent, string text, ref int position)
    {
        if (text[position] != '[')
        {
            return false;
        }

        var labelEnd = text.IndexOf("](", position + 1, StringComparison.Ordinal);
        var targetEnd = labelEnd < 0 ? -1 : text.IndexOf(')', labelEnd + 2);
        if (labelEnd < 0 || targetEnd < 0)
        {
            return false;
        }

        var link = new LinkNode(text[(labelEnd + 2)..targetEnd]);
        AddInlineNodes(link, text[(position + 1)..labelEnd]);
        parent.AddChild(link);
        position = targetEnd + 1;
        return true;
    }

    private static bool TryAddDelimited(
        RichTextNode parent,
        string text,
        ref int position,
        string delimiter,
        Func<RichTextNode> createNode)
    {
        if (!text.AsSpan(position).StartsWith(delimiter, StringComparison.Ordinal))
        {
            return false;
        }

        var contentStart = position + delimiter.Length;
        var contentEnd = text.IndexOf(delimiter, contentStart, StringComparison.Ordinal);
        if (contentEnd < 0)
        {
            return false;
        }

        var node = createNode();
        AddInlineNodes(node, text[contentStart..contentEnd]);
        parent.AddChild(node);
        position = contentEnd + delimiter.Length;
        return true;
    }

    private static bool TryAddInlineCode(
        RichTextNode parent,
        string text,
        ref int position)
    {
        if (text[position] != '`')
        {
            return false;
        }

        var contentEnd = text.IndexOf('`', position + 1);
        if (contentEnd < 0)
        {
            return false;
        }

        parent.AddChild(new InlineCodeNode(text[(position + 1)..contentEnd]));
        position = contentEnd + 1;
        return true;
    }

    private static int FindNextMarker(string text, int start)
    {
        var result = -1;
        foreach (var marker in new[] { "![", "[", "**", "~~", "*", "`" })
        {
            var index = text.IndexOf(marker, start, StringComparison.Ordinal);
            if (index >= 0 && (result < 0 || index < result))
            {
                result = index;
            }
        }
        return result;
    }
}
