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
        paragraph.AddChild(new EmphasisNode());
        paragraph.AddChild(new TextNode("B"));

        Assert.Equal(3, paragraph.Children.Count);
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

    [Fact]
    public void HeadingNode_DefaultLevel_Is1()
    {
        var node = new HeadingNode();
        Assert.Equal(1, node.Level);
    }

    [Fact]
    public void HeadingNode_ConstructorSetsLevel()
    {
        var node = new HeadingNode(3);
        Assert.Equal(3, node.Level);
    }

    [Fact]
    public void CodeBlockNode_StoresCodeAndLanguage()
    {
        var node = new CodeBlockNode("var x = 1;", "csharp");
        Assert.Equal("var x = 1;", node.Code);
        Assert.Equal("csharp", node.Language);
    }

    [Fact]
    public void CodeBlockNode_LanguageNullByDefault()
    {
        var node = new CodeBlockNode();
        Assert.Null(node.Language);
        Assert.Equal(string.Empty, node.Code);
    }

    [Fact]
    public void ListNode_StoresOrderedAndStart()
    {
        var node = new ListNode(ordered: true, start: 5);
        Assert.True(node.Ordered);
        Assert.Equal(5, node.Start);
    }

    [Fact]
    public void ListItemNode_CheckedNullByDefault()
    {
        var node = new ListItemNode();
        Assert.Null(node.Checked);
    }

    [Fact]
    public void ListItemNode_TaskItem()
    {
        var node = new ListItemNode { Checked = true };
        Assert.True(node.Checked);
    }

    [Fact]
    public void TableNode_AlignmentEmptyByDefault()
    {
        var node = new TableNode();
        Assert.Empty(node.Alignment);
    }

    [Fact]
    public void LinkNode_StoresUrlAndTitle()
    {
        var node = new LinkNode("https://example.com", "Example");
        Assert.Equal("https://example.com", node.Url);
        Assert.Equal("Example", node.Title);
    }

    [Fact]
    public void ImageNode_StoresUrlAltTitle()
    {
        var node = new ImageNode("https://img.png", "alt text", "title");
        Assert.Equal("https://img.png", node.Url);
        Assert.Equal("alt text", node.Alt);
        Assert.Equal("title", node.Title);
    }

    [Fact]
    public void InlineCodeNode_StoresCode()
    {
        var node = new InlineCodeNode("Console.WriteLine");
        Assert.Equal("Console.WriteLine", node.Code);
    }

    [Fact]
    public void HtmlNode_StoresValue()
    {
        var node = new HtmlNode("<div>test</div>");
        Assert.Equal("<div>test</div>", node.Value);
    }

    [Fact]
    public void DefinitionNode_StoresLabelUrlTitle()
    {
        var node = new DefinitionNode
        {
            Label = "example",
            Url = "https://example.com",
            Title = "Example Site"
        };
        Assert.Equal("example", node.Label);
        Assert.Equal("https://example.com", node.Url);
        Assert.Equal("Example Site", node.Title);
    }

    [Fact]
    public void LinkReferenceNode_StoresLabelAndReferenceKind()
    {
        var node = new LinkReferenceNode
        {
            Label = "ref1",
            ReferenceKind = ReferenceKind.Full
        };
        Assert.Equal("ref1", node.Label);
        Assert.Equal(ReferenceKind.Full, node.ReferenceKind);
    }

    [Fact]
    public void FootnoteReferenceNode_StoresLabel()
    {
        var node = new FootnoteReferenceNode { Label = "fn1" };
        Assert.Equal("fn1", node.Label);
    }

    [Fact]
    public void FootnoteDefinitionNode_StoresLabel()
    {
        var node = new FootnoteDefinitionNode { Label = "fn1" };
        Assert.Equal("fn1", node.Label);
    }

    [Fact]
    public void ComplexTree_CanBeBuilt()
    {
        // Build: paragraph with "Hello " + emphasis("world")
        var paragraph = new ParagraphNode();
        paragraph.AddChild(new TextNode("Hello "));

        var emphasis = new EmphasisNode();
        emphasis.AddChild(new TextNode("world"));
        paragraph.AddChild(emphasis);

        Assert.Equal(2, paragraph.Children.Count);
        Assert.IsType<TextNode>(paragraph.Children[0]);
        var em = Assert.IsType<EmphasisNode>(paragraph.Children[1]);
        Assert.Equal("world", Assert.IsType<TextNode>(Assert.Single(em.Children)).Text);
    }

    [Fact]
    public void NestedStructure_BlockQuoteWithList()
    {
        var quote = new BlockQuoteNode();
        var list = new ListNode(ordered: false);
        var item = new ListItemNode();
        var para = new ParagraphNode();
        para.AddChild(new TextNode("Item text"));
        item.AddChild(para);
        list.AddChild(item);
        quote.AddChild(list);

        Assert.Single(quote.Children);
        var innerList = Assert.IsType<ListNode>(quote.Children[0]);
        Assert.False(innerList.Ordered);
        Assert.Single(innerList.Children);
    }
}
