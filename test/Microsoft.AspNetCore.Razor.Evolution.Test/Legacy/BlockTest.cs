// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    internal class BlockTest
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
    }
}
