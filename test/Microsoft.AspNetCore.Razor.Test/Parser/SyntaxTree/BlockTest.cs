// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Web.WebPages.TestUtils;
using Microsoft.AspNetCore.Razor.Chunks.Generators;
using Microsoft.AspNetCore.Razor.Test.Framework;
using Microsoft.AspNetCore.Razor.Text;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Parser.SyntaxTree
{
    public class BlockTest
    {
        [Fact]
        public void ConstructorWithBlockBuilderSetsParent()
        {
            // Arrange
            var builder = new BlockBuilder() { Type = BlockType.Comment };
            var span = new SpanBuilder() { Kind = SpanKind.Code }.Build();
            builder.Children.Add(span);

            // Act
            var block = builder.Build();

            // Assert
            Assert.Same(block, span.Parent);
        }

        [Fact]
        public void ConstructorTransfersInstanceOfChunkGeneratorFromBlockBuilder()
        {
            // Arrange
            var expected = new ExpressionChunkGenerator();
            var builder = new BlockBuilder()
            {
                Type = BlockType.Helper,
                ChunkGenerator = expected
            };

            // Act
            var actual = builder.Build();

            // Assert
            Assert.Same(expected, actual.ChunkGenerator);
        }

        [Fact]
        public void ConstructorTransfersChildrenFromBlockBuilder()
        {
            // Arrange
            var expected = new SpanBuilder() { Kind = SpanKind.Code }.Build();
            var builder = new BlockBuilder()
            {
                Type = BlockType.Functions
            };
            builder.Children.Add(expected);

            // Act
            var block = builder.Build();

            // Assert
            Assert.Same(expected, block.Children.Single());
        }

        [Fact]
        public void LocateOwnerReturnsNullIfNoSpanReturnsTrueForOwnsSpan()
        {
            // Arrange
            var factory = SpanFactory.CreateCsHtml();
            var block = new MarkupBlock(
                factory.Markup("Foo "),
                new StatementBlock(
                    factory.CodeTransition(),
                    factory.Code("bar").AsStatement()),
                factory.Markup(" Baz"));
            var change = new TextChange(128, 1, new StringTextBuffer("Foo @bar Baz"), 1, new StringTextBuffer("Foo @bor Baz"));

            // Act
            var actual = block.LocateOwner(change);

            // Assert
            Assert.Null(actual);
        }

        [Fact]
        public void LocateOwnerReturnsNullIfChangeCrossesMultipleSpans()
        {
            // Arrange
            var factory = SpanFactory.CreateCsHtml();
            var block = new MarkupBlock(
                factory.Markup("Foo "),
                new StatementBlock(
                    factory.CodeTransition(),
                    factory.Code("bar").AsStatement()),
                factory.Markup(" Baz"));
            var change = new TextChange(4, 10, new StringTextBuffer("Foo @bar Baz"), 10, new StringTextBuffer("Foo @bor Baz"));

            // Act
            var actual = block.LocateOwner(change);

            // Assert
            Assert.Null(actual);
        }
    }
}
