// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    public class TagHelperBlockTest
    {
        [Fact]
        public void FlattenFlattensSelfClosingTagHelpers()
        {
            // Arrange
            var spanFactory = new SpanFactory();
            var blockFactory = new BlockFactory(spanFactory);
            var tagHelper = (TagHelperBlock)blockFactory.TagHelperBlock(
                tagName: "input",
                tagMode: TagMode.SelfClosing,
                start: SourceLocation.Zero,
                startTag: blockFactory.MarkupTagBlock("<input />"),
                children: new SyntaxTreeNode[0],
                endTag: null);
            spanFactory.Reset();
            var expectedNode = spanFactory.Markup("<input />");

            // Act
            var flattenedNodes = tagHelper.Flatten();

            // Assert
            var node = Assert.Single(flattenedNodes);
            Assert.True(node.EquivalentTo(expectedNode));
        }

        [Fact]
        public void FlattenFlattensStartAndEndTagTagHelpers()
        {
            // Arrange
            var spanFactory = new SpanFactory();
            var blockFactory = new BlockFactory(spanFactory);
            var tagHelper = (TagHelperBlock)blockFactory.TagHelperBlock(
                tagName: "div",
                tagMode: TagMode.StartTagAndEndTag,
                start: SourceLocation.Zero,
                startTag: blockFactory.MarkupTagBlock("<div>"),
                children: new SyntaxTreeNode[0],
                endTag: blockFactory.MarkupTagBlock("</div>"));
            spanFactory.Reset();
            var expectedStartTag = spanFactory.Markup("<div>");
            var expectedEndTag = spanFactory.Markup("</div>");

            // Act
            var flattenedNodes = tagHelper.Flatten();

            // Assert
            Assert.Collection(
                flattenedNodes,
                first =>
                {
                    Assert.True(first.EquivalentTo(expectedStartTag));
                },
                second =>
                {
                    Assert.True(second.EquivalentTo(expectedEndTag));
                });
        }

        [Fact]
        public void FlattenFlattensStartAndEndTagWithChildrenTagHelpers()
        {
            // Arrange
            var spanFactory = new SpanFactory();
            var blockFactory = new BlockFactory(spanFactory);
            var tagHelper = (TagHelperBlock)blockFactory.TagHelperBlock(
                tagName: "div",
                tagMode: TagMode.StartTagAndEndTag,
                start: SourceLocation.Zero,
                startTag: blockFactory.MarkupTagBlock("<div>"),
                children: new SyntaxTreeNode[] { spanFactory.Markup("Hello World") },
                endTag: blockFactory.MarkupTagBlock("</div>"));
            spanFactory.Reset();
            var expectedStartTag = spanFactory.Markup("<div>");
            var expectedChildren = spanFactory.Markup("Hello World");
            var expectedEndTag = spanFactory.Markup("</div>");

            // Act
            var flattenedNodes = tagHelper.Flatten();

            // Assert
            Assert.Collection(
                flattenedNodes,
                first =>
                {
                    Assert.True(first.EquivalentTo(expectedStartTag));
                },
                second =>
                {
                    Assert.True(second.EquivalentTo(expectedChildren));
                },
                third =>
                {
                    Assert.True(third.EquivalentTo(expectedEndTag));
                });
        }
    }
}
