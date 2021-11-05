// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal static class NodeAssert
{
    public static HtmlAttributeIntermediateNode Attribute(IntermediateNode node, string attributeName, string attributeValue)
    {
        Assert.NotNull(node);

        var attributeNode = Assert.IsType<HtmlAttributeIntermediateNode>(node);
        Assert.Equal(attributeName, attributeNode.AttributeName);

        var attributeValueNode = Assert.IsType<HtmlAttributeValueIntermediateNode>(Assert.Single(attributeNode.Children));
        var actual = new StringBuilder();
        for (var i = 0; i < attributeValueNode.Children.Count; i++)
        {
            var token = Assert.IsAssignableFrom<IntermediateToken>(attributeValueNode.Children[i]);
            Assert.Equal(TokenKind.Html, token.Kind);
            actual.Append(token.Content);
        }

        Assert.Equal(attributeValue, actual.ToString());

        return attributeNode;
    }

    public static HtmlAttributeIntermediateNode Attribute(IntermediateNodeCollection nodes, string attributeName, string attributeValue)
    {
        Assert.NotNull(nodes);
        return Attribute(Assert.Single(nodes), attributeName, attributeValue);
    }

    public static HtmlContentIntermediateNode Content(IntermediateNode node, string content, bool trim = true)
    {
        Assert.NotNull(node);

        var contentNode = Assert.IsType<HtmlContentIntermediateNode>(node);

        var actual = new StringBuilder();
        for (var i = 0; i < contentNode.Children.Count; i++)
        {
            var token = Assert.IsAssignableFrom<IntermediateToken>(contentNode.Children[i]);
            Assert.Equal(TokenKind.Html, token.Kind);
            actual.Append(token.Content);
        }

        Assert.Equal(content, trim ? actual.ToString().Trim() : actual.ToString());
        return contentNode;
    }

    public static HtmlContentIntermediateNode Content(IntermediateNodeCollection nodes, string content, bool trim = true)
    {
        Assert.NotNull(nodes);
        return Content(Assert.Single(nodes), content, trim);
    }

    public static HtmlAttributeIntermediateNode CSharpAttribute(IntermediateNode node, string attributeName, string attributeValue)
    {
        Assert.NotNull(node);

        var attributeNode = Assert.IsType<HtmlAttributeIntermediateNode>(node);
        Assert.Equal(attributeName, attributeNode.AttributeName);

        var attributeValueNode = Assert.IsType<CSharpExpressionAttributeValueIntermediateNode>(Assert.Single(attributeNode.Children));
        var actual = new StringBuilder();
        for (var i = 0; i < attributeValueNode.Children.Count; i++)
        {
            var token = Assert.IsAssignableFrom<IntermediateToken>(attributeValueNode.Children[i]);
            Assert.Equal(TokenKind.CSharp, token.Kind);
            actual.Append(token.Content);
        }

        Assert.Equal(attributeValue, actual.ToString());

        return attributeNode;
    }

    public static HtmlAttributeIntermediateNode CSharpAttribute(IntermediateNodeCollection nodes, string attributeName, string attributeValue)
    {
        Assert.NotNull(nodes);
        return Attribute(Assert.Single(nodes), attributeName, attributeValue);
    }

    public static MarkupElementIntermediateNode Element(IntermediateNode node, string tagName)
    {
        Assert.NotNull(node);

        var elementNode = Assert.IsType<MarkupElementIntermediateNode>(node);
        Assert.Equal(tagName, elementNode.TagName);
        return elementNode;
    }

    public static MarkupElementIntermediateNode Element(IntermediateNodeCollection nodes, string tagName)
    {
        Assert.NotNull(nodes);
        return Element(Assert.Single(nodes), tagName);
    }

    public static HtmlContentIntermediateNode Whitespace(IntermediateNode node)
    {
        Assert.NotNull(node);

        var contentNode = Assert.IsType<HtmlContentIntermediateNode>(node);
        for (var i = 0; i < contentNode.Children.Count; i++)
        {
            var token = Assert.IsAssignableFrom<IntermediateToken>(contentNode.Children[i]);
            Assert.Equal(TokenKind.Html, token.Kind);
            Assert.True(string.IsNullOrWhiteSpace(token.Content));
        }

        return contentNode;
    }

    public static HtmlContentIntermediateNode Whitespace(IntermediateNodeCollection nodes)
    {
        Assert.NotNull(nodes);
        return Whitespace(Assert.Single(nodes));
    }
}
