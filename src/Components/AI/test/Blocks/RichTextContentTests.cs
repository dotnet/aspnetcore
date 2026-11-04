// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI.Tests.Blocks;

public class RichTextContentTests
{
    [Fact]
    public void Constructor_CopiesNodeList()
    {
        var nodes = new List<RichTextNode>
        {
            new ParagraphNode(),
        };

        var content = new RichTextContent("text", nodes);
        nodes.Clear();

        Assert.Single(content.Nodes);
        Assert.Equal("text", content.Text);
    }

    [Fact]
    public void Constructor_NullTextThrows()
    {
        Assert.Throws<ArgumentNullException>(
            () => new RichTextContent(null!, Array.Empty<RichTextNode>()));
    }

    [Fact]
    public void Constructor_NullNodesThrows()
    {
        Assert.Throws<ArgumentNullException>(() => new RichTextContent("text", null!));
    }
}
