// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class BlockTest
    {
        [Fact]
        public void ChildChanged_NotifiesParent()
        {
            // Arrange
            var spanBuilder = new SpanBuilder(SourceLocation.Zero);
            spanBuilder.Accept(new HtmlSymbol("hello", HtmlSymbolType.Text));
            var span = spanBuilder.Build();
            var blockBuilder = new BlockBuilder()
            {
                Type = BlockKindInternal.Markup,
            };
            blockBuilder.Children.Add(span);
            var childBlock = blockBuilder.Build();
            blockBuilder = new BlockBuilder()
            {
                Type = BlockKindInternal.Markup,
            };
            blockBuilder.Children.Add(childBlock);
            var parentBlock = blockBuilder.Build();
            var originalBlockLength = parentBlock.Length;
            spanBuilder = new SpanBuilder(SourceLocation.Zero);
            spanBuilder.Accept(new HtmlSymbol("hi", HtmlSymbolType.Text));
            span.ReplaceWith(spanBuilder);
            
            // Wire up parents now so we can re-trigger ChildChanged to cause cache refresh.
            span.Parent = childBlock;
            childBlock.Parent = parentBlock;

            // Act
            childBlock.ChildChanged();

            // Assert
            Assert.Equal(5, originalBlockLength);
            Assert.Equal(2, parentBlock.Length);
        }

        [Fact]
        public void ConstructorWithBlockBuilderSetsParent()
        {
            // Arrange
            var builder = new BlockBuilder() { Type = BlockKindInternal.Comment };
            var span = new SpanBuilder(SourceLocation.Undefined) { Kind = SpanKindInternal.Code }.Build();
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
                Type = BlockKindInternal.Statement,
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
            var expected = new SpanBuilder(SourceLocation.Undefined) { Kind = SpanKindInternal.Code }.Build();
            var builder = new BlockBuilder()
            {
                Type = BlockKindInternal.Statement
            };
            builder.Children.Add(expected);

            // Act
            var block = builder.Build();

            // Assert
            Assert.Same(expected, block.Children.Single());
        }
    }
}
