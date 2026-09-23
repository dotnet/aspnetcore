// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI.Tests.Blocks;

public class RichTextNodeTests
{
    [Fact]
    public void AddChild_BuildsNestedTree()
    {
        var paragraph = new ParagraphNode();
        paragraph.AddChild(new TextNode("Hello "));
        var emphasis = new EmphasisNode();
        emphasis.AddChild(new TextNode("world"));
        paragraph.AddChild(emphasis);

        Assert.Equal(2, paragraph.Children.Count);
        Assert.Equal("Hello ", Assert.IsType<TextNode>(paragraph.Children[0]).Text);
        Assert.Equal(
            "world",
            Assert.IsType<TextNode>(Assert.Single(
                Assert.IsType<EmphasisNode>(paragraph.Children[1]).Children)).Text);
    }

    [Fact]
    public void AddChild_NullThrows()
    {
        Assert.Throws<ArgumentNullException>(() => new ParagraphNode().AddChild(null!));
    }

    [Fact]
    public void Constructors_PreserveValues()
    {
        Assert.Equal("text", new TextNode("text").Text);
        Assert.Equal(3, new HeadingNode(3).Level);

        var codeBlock = new CodeBlockNode("var value = 1;", "csharp");
        Assert.Equal("var value = 1;", codeBlock.Code);
        Assert.Equal("csharp", codeBlock.Language);

        var link = new LinkNode("https://example.com", "Example");
        Assert.Equal("https://example.com", link.Url);
        Assert.Equal("Example", link.Title);

        var image = new ImageNode("/image.svg", "alt text", "Image");
        Assert.Equal("/image.svg", image.Url);
        Assert.Equal("alt text", image.Alt);
        Assert.Equal("Image", image.Title);

        Assert.Equal("code", new InlineCodeNode("code").Code);
        Assert.Equal("<strong>text</strong>", new HtmlNode("<strong>text</strong>").Value);
    }

    [Fact]
    public void ListAndTableMetadata_PreserveValues()
    {
        var list = new ListNode(ordered: true, start: 5);
        Assert.True(list.Ordered);
        Assert.Equal(5, list.Start);

        var item = new ListItemNode { Checked = true };
        Assert.True(item.Checked);

        var table = new TableNode
        {
            Alignment =
            [
                TableColumnAlignment.Left,
                TableColumnAlignment.Center,
                TableColumnAlignment.Right,
            ],
        };
        Assert.Equal(3, table.Alignment.Count);
    }

    [Fact]
    public void ReferenceMetadata_PreserveValues()
    {
        var definition = new DefinitionNode
        {
            Label = "docs",
            Url = "https://example.com",
            Title = "Documentation",
        };
        Assert.Equal("docs", definition.Label);
        Assert.Equal("https://example.com", definition.Url);
        Assert.Equal("Documentation", definition.Title);

        var link = new LinkReferenceNode
        {
            Label = "docs",
            ReferenceKind = ReferenceKind.Full,
        };
        Assert.Equal("docs", link.Label);
        Assert.Equal(ReferenceKind.Full, link.ReferenceKind);

        var image = new ImageReferenceNode
        {
            Label = "logo",
            Alt = "Logo",
            ReferenceKind = ReferenceKind.Collapsed,
        };
        Assert.Equal("logo", image.Label);
        Assert.Equal("Logo", image.Alt);
        Assert.Equal(ReferenceKind.Collapsed, image.ReferenceKind);

        Assert.Equal("note", new FootnoteReferenceNode { Label = "note" }.Label);
        Assert.Equal("note", new FootnoteDefinitionNode { Label = "note" }.Label);
    }
}
