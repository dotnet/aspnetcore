// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI.Tests.Blocks;

public class RichTextNodeTests
{
    [Fact]
    public void Children_EmptyByDefault()
    {
        var node = new ParagraphNode();
        Assert.Empty(node.Children);
    }

    [Fact]
    public void AddChild_AddsToChildren()
    {
        var paragraph = new ParagraphNode();
        var text = new TextNode("Hello");
        paragraph.AddChild(text);

        Assert.Single(paragraph.Children);
        Assert.Same(text, paragraph.Children[0]);
    }

    [Fact]
    public void AddChild_MultipleChildren()
    {
        var paragraph = new ParagraphNode();
        paragraph.AddChild(new TextNode("A"));
        paragraph.AddChild(new TextNode("B"));

        Assert.Equal(2, paragraph.Children.Count);
    }

    [Fact]
    public void TextNode_StoresText()
    {
        var node = new TextNode("Hello world");
        Assert.Equal("Hello world", node.Text);
    }

    [Fact]
    public void TextNode_DefaultText_IsEmpty()
    {
        var node = new TextNode();
        Assert.Equal(string.Empty, node.Text);
    }

}
