// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI.Tests.Blocks;

public class RichContentBlockTests
{
    [Fact]
    public void AppendText_AccumulatesTokens()
    {
        var block = new RichContentBlock();
        block.AppendText("Hello");
        block.AppendText(", ");
        block.AppendText("world!");

        Assert.Equal("Hello, world!", block.RawText);
    }

    [Fact]
    public void RawText_EmptyByDefault()
    {
        var block = new RichContentBlock();
        Assert.Equal(string.Empty, block.RawText);
    }

    [Fact]
    public void Content_EmptyByDefault()
    {
        var block = new RichContentBlock();
        Assert.Empty(block.Content);
    }

    [Fact]
    public void RawText_CachesResult()
    {
        var block = new RichContentBlock();
        block.AppendText("Hello");

        var first = block.RawText;
        var second = block.RawText;

        Assert.Same(first, second);
    }

    [Fact]
    public void RawText_InvalidatesCacheOnAppend()
    {
        var block = new RichContentBlock();
        block.AppendText("Hello");
        var first = block.RawText;

        block.AppendText(" world");
        var second = block.RawText;

        Assert.NotSame(first, second);
        Assert.Equal("Hello world", second);
    }
}
