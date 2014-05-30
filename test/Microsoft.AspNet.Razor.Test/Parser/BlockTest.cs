// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Web.WebPages.TestUtils;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Test.Framework;
using Microsoft.AspNet.Razor.Text;
using Xunit;

namespace Microsoft.AspNet.Razor.Test.Parser
{
    public class BlockTest
    {
        [Fact]
        public void ConstructorWithBlockBuilderSetsParent()
        {
            // Arrange
            BlockBuilder builder = new BlockBuilder() { Type = BlockType.Comment };
            Span span = new SpanBuilder() { Kind = SpanKind.Code }.Build();
            builder.Children.Add(span);

            // Act
            Block block = builder.Build();

            // Assert
            Assert.Same(block, span.Parent);
        }

        [Fact]
        public void ConstructorCopiesBasicValuesFromBlockBuilder()
        {
            // Arrange
            BlockBuilder builder = new BlockBuilder()
            {
                Name = "Foo",
                Type = BlockType.Helper
            };

            // Act
            Block actual = builder.Build();

            // Assert
            Assert.Equal("Foo", actual.Name);
            Assert.Equal(BlockType.Helper, actual.Type);
        }

        [Fact]
        public void ConstructorTransfersInstanceOfCodeGeneratorFromBlockBuilder()
        {
            // Arrange
            IBlockCodeGenerator expected = new ExpressionCodeGenerator();
            BlockBuilder builder = new BlockBuilder()
            {
                Type = BlockType.Helper,
                CodeGenerator = expected
            };

            // Act
            Block actual = builder.Build();

            // Assert
            Assert.Same(expected, actual.CodeGenerator);
        }

        [Fact]
        public void ConstructorTransfersChildrenFromBlockBuilder()
        {
            // Arrange
            Span expected = new SpanBuilder() { Kind = SpanKind.Code }.Build();
            BlockBuilder builder = new BlockBuilder()
            {
                Type = BlockType.Functions
            };
            builder.Children.Add(expected);

            // Act
            Block block = builder.Build();

            // Assert
            Assert.Same(expected, block.Children.Single());
        }

        [Fact]
        public void LocateOwnerReturnsNullIfNoSpanReturnsTrueForOwnsSpan()
        {
            // Arrange
            var factory = SpanFactory.CreateCsHtml();
            Block block = new MarkupBlock(
                factory.Markup("Foo "),
                new StatementBlock(
                    factory.CodeTransition(),
                    factory.Code("bar").AsStatement()),
                factory.Markup(" Baz"));
            TextChange change = new TextChange(128, 1, new StringTextBuffer("Foo @bar Baz"), 1, new StringTextBuffer("Foo @bor Baz"));

            // Act
            Span actual = block.LocateOwner(change);

            // Assert
            Assert.Null(actual);
        }

        [Fact]
        public void LocateOwnerReturnsNullIfChangeCrossesMultipleSpans()
        {
            // Arrange
            var factory = SpanFactory.CreateCsHtml();
            Block block = new MarkupBlock(
                factory.Markup("Foo "),
                new StatementBlock(
                    factory.CodeTransition(),
                    factory.Code("bar").AsStatement()),
                factory.Markup(" Baz"));
            TextChange change = new TextChange(4, 10, new StringTextBuffer("Foo @bar Baz"), 10, new StringTextBuffer("Foo @bor Baz"));

            // Act
            Span actual = block.LocateOwner(change);

            // Assert
            Assert.Null(actual);
        }
    }
}
