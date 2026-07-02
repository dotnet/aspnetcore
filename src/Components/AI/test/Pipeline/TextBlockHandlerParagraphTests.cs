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

        var paragraph = Assert.IsType<ParagraphNode>(Assert.Single(block.Content));
        var text = Assert.IsType<TextNode>(Assert.Single(paragraph.Children));
        Assert.Equal("Hello world", text.Text);
    }

    [Fact]
    public void RebuildParagraphs_TwoParagraphs()
    {
        var block = new RichContentBlock();
        block.AppendText("First\n\nSecond");
        TextBlockHandler.RebuildParagraphs(block);

        Assert.Equal(2, block.Content.Count);

        var p1 = Assert.IsType<ParagraphNode>(block.Content[0]);
        Assert.Equal("First", Assert.IsType<TextNode>(Assert.Single(p1.Children)).Text);

        var p2 = Assert.IsType<ParagraphNode>(block.Content[1]);
        Assert.Equal("Second", Assert.IsType<TextNode>(Assert.Single(p2.Children)).Text);
    }

    [Fact]
    public void RebuildParagraphs_ThreeParagraphs()
    {
        var block = new RichContentBlock();
        block.AppendText("A\n\nB\n\nC");
        TextBlockHandler.RebuildParagraphs(block);

        Assert.Equal(3, block.Content.Count);
        Assert.Equal("A", Assert.IsType<TextNode>(Assert.Single(Assert.IsType<ParagraphNode>(block.Content[0]).Children)).Text);
        Assert.Equal("B", Assert.IsType<TextNode>(Assert.Single(Assert.IsType<ParagraphNode>(block.Content[1]).Children)).Text);
        Assert.Equal("C", Assert.IsType<TextNode>(Assert.Single(Assert.IsType<ParagraphNode>(block.Content[2]).Children)).Text);
    }

    [Fact]
    public void RebuildParagraphs_TrailingDoubleNewline_NoEmptyParagraph()
    {
        var block = new RichContentBlock();
        block.AppendText("Hello\n\n");
        TextBlockHandler.RebuildParagraphs(block);

        var paragraph = Assert.IsType<ParagraphNode>(Assert.Single(block.Content));
        Assert.Equal("Hello", Assert.IsType<TextNode>(Assert.Single(paragraph.Children)).Text);
    }

    [Fact]
    public void RebuildParagraphs_SingleNewlineNotAParagraphBreak()
    {
        var block = new RichContentBlock();
        block.AppendText("Line one\nLine two");
        TextBlockHandler.RebuildParagraphs(block);

        // Single \n is NOT a paragraph break
        var paragraph = Assert.IsType<ParagraphNode>(Assert.Single(block.Content));
        Assert.Equal("Line one\nLine two", Assert.IsType<TextNode>(Assert.Single(paragraph.Children)).Text);
    }

    [Fact]
    public void RebuildParagraphs_EmptyText_NoContent()
    {
        var block = new RichContentBlock();
        TextBlockHandler.RebuildParagraphs(block);

        Assert.Empty(block.Content);
    }

    [Fact]
    public void RebuildParagraphs_OnlyDoubleNewlines_NoContent()
    {
        var block = new RichContentBlock();
        block.AppendText("\n\n\n\n");
        TextBlockHandler.RebuildParagraphs(block);

        Assert.Empty(block.Content);
    }

    [Fact]
    public void RebuildParagraphs_StreamingTokens_ParagraphsRebuilt()
    {
        var block = new RichContentBlock();

        block.AppendText("Hello");
        TextBlockHandler.RebuildParagraphs(block);
        Assert.Single(block.Content);

        block.AppendText(" world");
        TextBlockHandler.RebuildParagraphs(block);
        Assert.Single(block.Content);

        block.AppendText("\n\n");
        TextBlockHandler.RebuildParagraphs(block);
        Assert.Single(block.Content); // trailing \n\n, no second paragraph yet

        block.AppendText("Second");
        TextBlockHandler.RebuildParagraphs(block);
        Assert.Equal(2, block.Content.Count);

        Assert.Equal("Hello world", Assert.IsType<TextNode>(
            Assert.Single(Assert.IsType<ParagraphNode>(block.Content[0]).Children)).Text);
        Assert.Equal("Second", Assert.IsType<TextNode>(
            Assert.Single(Assert.IsType<ParagraphNode>(block.Content[1]).Children)).Text);
    }

    [Fact]
    public void RebuildParagraphs_TrimsTrailingNewlinesFromParagraphs()
    {
        var block = new RichContentBlock();
        block.AppendText("First\n\n\nSecond");
        TextBlockHandler.RebuildParagraphs(block);

        // The extra \n after the break is part of "Second" paragraph, not trimmed
        Assert.Equal(2, block.Content.Count);
    }
}
