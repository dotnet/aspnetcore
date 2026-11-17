// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI.Tests.Pipeline;

public class TextBlockHandlerParagraphTests
{
    [Fact]
    public void RebuildParagraphs_SingleText_OneParagraph()
    {
        var block = new RichContentBlock();
        block.AppendText("Hello world");

        TextBlockHandler.RebuildParagraphs(block);

        Assert.Equal("Hello world", GetParagraphText(Assert.Single(block.Content)));
    }

    [Fact]
    public void RebuildParagraphs_TwoParagraphs()
    {
        var block = new RichContentBlock();
        block.AppendText("First\n\nSecond");

        TextBlockHandler.RebuildParagraphs(block);

        Assert.Equal(["First", "Second"], block.Content.Select(GetParagraphText));
    }

    [Fact]
    public void RebuildParagraphs_TrailingDoubleNewline_NoEmptyParagraph()
    {
        var block = new RichContentBlock();
        block.AppendText("Hello\n\n");

        TextBlockHandler.RebuildParagraphs(block);

        Assert.Equal("Hello", GetParagraphText(Assert.Single(block.Content)));
    }

    [Fact]
    public void RebuildParagraphs_ConsecutiveBlankLines_NoEmptyParagraph()
    {
        var block = new RichContentBlock();
        block.AppendText("First\n\n\n\nSecond");

        TextBlockHandler.RebuildParagraphs(block);

        Assert.Equal(["First", "Second"], block.Content.Select(GetParagraphText));
    }

    [Fact]
    public void RebuildParagraphs_EmptyText_NoParagraphs()
    {
        var block = new RichContentBlock();

        TextBlockHandler.RebuildParagraphs(block);

        Assert.Empty(block.Content);
    }

    [Fact]
    public void RebuildParagraphs_SingleNewline_StaysInSameParagraph()
    {
        var block = new RichContentBlock();
        block.AppendText("First\nSecond");

        TextBlockHandler.RebuildParagraphs(block);

        Assert.Equal("First\nSecond", GetParagraphText(Assert.Single(block.Content)));
    }

    private static string GetParagraphText(RichTextNode node)
    {
        var paragraph = Assert.IsType<ParagraphNode>(node);
        return Assert.IsType<TextNode>(Assert.Single(paragraph.Children)).Text;
    }
}
