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

        Assert.Equal("Hello world", Assert.Single(block.Paragraphs));
    }

    [Fact]
    public void RebuildParagraphs_TwoParagraphs()
    {
        var block = new RichContentBlock();
        block.AppendText("First\n\nSecond");

        TextBlockHandler.RebuildParagraphs(block);

        Assert.Equal(["First", "Second"], block.Paragraphs);
    }

    [Fact]
    public void RebuildParagraphs_TrailingDoubleNewline_NoEmptyParagraph()
    {
        var block = new RichContentBlock();
        block.AppendText("Hello\n\n");

        TextBlockHandler.RebuildParagraphs(block);

        Assert.Equal("Hello", Assert.Single(block.Paragraphs));
    }

    [Fact]
    public void RebuildParagraphs_ConsecutiveBlankLines_NoEmptyParagraph()
    {
        var block = new RichContentBlock();
        block.AppendText("First\n\n\n\nSecond");

        TextBlockHandler.RebuildParagraphs(block);

        Assert.Equal(["First", "Second"], block.Paragraphs);
    }

    [Fact]
    public void RebuildParagraphs_EmptyText_NoParagraphs()
    {
        var block = new RichContentBlock();

        TextBlockHandler.RebuildParagraphs(block);

        Assert.Empty(block.Paragraphs);
    }

    [Fact]
    public void RebuildParagraphs_SingleNewline_StaysInSameParagraph()
    {
        var block = new RichContentBlock();
        block.AppendText("First\nSecond");

        TextBlockHandler.RebuildParagraphs(block);

        Assert.Equal("First\nSecond", Assert.Single(block.Paragraphs));
    }
}
