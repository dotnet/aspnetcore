// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class TagHelperBlockTest
    {
        [Fact]
        public void Clone_ClonesTagHelperChildren()
        {
            // Arrange
            var tagHelper = new TagHelperBlockBuilder(
                "p",
                TagMode.StartTagAndEndTag,
                attributes: new List<TagHelperAttributeNode>(),
                children: new[]
            {
                new SpanBuilder(SourceLocation.Zero).Build(),
                new SpanBuilder(new SourceLocation(0, 1, 2)).Build(),
            }).Build();

            // Act
            var copy = (TagHelperBlock)tagHelper.Clone();

            // Assert
            ParserTestBase.EvaluateParseTree(copy, tagHelper);
            Assert.Collection(
                copy.Children,
                child => Assert.NotSame(tagHelper.Children[0], child),
                child => Assert.NotSame(tagHelper.Children[1], child));
        }

        [Fact]
        public void Clone_ClonesTagHelperAttributes()
        {
            // Arrange
            var tagHelper = (TagHelperBlock)new TagHelperBlockBuilder(
                "p",
                TagMode.StartTagAndEndTag,
                attributes: new List<TagHelperAttributeNode>()
                {
                    new TagHelperAttributeNode("class", new SpanBuilder(SourceLocation.Zero).Build(), AttributeStructure.NoQuotes),
                    new TagHelperAttributeNode("checked", new SpanBuilder(SourceLocation.Undefined).Build(), AttributeStructure.NoQuotes)
                },
                children: Enumerable.Empty<SyntaxTreeNode>()).Build();

            // Act
            var copy = (TagHelperBlock)tagHelper.Clone();

            // Assert
            ParserTestBase.EvaluateParseTree(copy, tagHelper);
            Assert.Collection(
                copy.Attributes,
                attribute => Assert.NotSame(tagHelper.Attributes[0], attribute),
                attribute => Assert.NotSame(tagHelper.Attributes[1], attribute));
        }

        [Fact]
        public void Clone_ClonesTagHelperSourceStartTag()
        {
            // Arrange
            var tagHelper = (TagHelperBlock)new TagHelperBlockBuilder(
                "p",
                TagMode.StartTagAndEndTag,
                attributes: new List<TagHelperAttributeNode>(),
                children: Enumerable.Empty<SyntaxTreeNode>())
            {
                SourceStartTag = new BlockBuilder()
                {
                    Type = BlockKindInternal.Comment,
                    ChunkGenerator = new RazorCommentChunkGenerator()
                }.Build()
            }.Build();

            // Act
            var copy = (TagHelperBlock)tagHelper.Clone();

            // Assert
            ParserTestBase.EvaluateParseTree(copy, tagHelper);
            Assert.NotSame(tagHelper.SourceStartTag, copy.SourceStartTag);
        }

        [Fact]
        public void Clone_ClonesTagHelperSourceEndTag()
        {
            // Arrange
            var tagHelper = (TagHelperBlock)new TagHelperBlockBuilder(
                "p",
                TagMode.StartTagAndEndTag,
                attributes: new List<TagHelperAttributeNode>(),
                children: Enumerable.Empty<SyntaxTreeNode>())
            {
                SourceEndTag = new BlockBuilder()
                {
                    Type = BlockKindInternal.Comment,
                    ChunkGenerator = new RazorCommentChunkGenerator()
                }.Build()
            }.Build();

            // Act
            var copy = (TagHelperBlock)tagHelper.Clone();

            // Assert
            ParserTestBase.EvaluateParseTree(copy, tagHelper);
            Assert.NotSame(tagHelper.SourceEndTag, copy.SourceEndTag);
        }

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
