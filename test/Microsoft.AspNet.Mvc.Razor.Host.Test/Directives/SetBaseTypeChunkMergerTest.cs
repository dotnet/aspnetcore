// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Chunks;
using Microsoft.AspNet.Testing;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor.Directives
{
    public class SetBaseTypeChunkMergerTest
    {
        [Fact]
        public void Visit_ThrowsIfThePassedInChunkIsNotASetBaseTypeChunk()
        {
            // Arrange
            var expected = "Argument must be an instance of " +
                           "'Microsoft.AspNet.Razor.Chunks.SetBaseTypeChunk'.";
            var merger = new SetBaseTypeChunkMerger("dynamic");

            // Act and Assert
            ExceptionAssert.ThrowsArgument(() => merger.VisitChunk(new LiteralChunk()), "chunk", expected);
        }

        [Theory]
        [InlineData("MyApp.BaseType<TModel>", "MyApp.BaseType<Person>")]
        [InlineData("TestBaseType", "TestBaseType")]
        public void Visit_UpdatesTModelTokenToMatchModelType(string typeName, string expectedValue)
        {
            // Arrange
            var chunk = new SetBaseTypeChunk
            {
                TypeName = typeName,
            };
            var merger = new SetBaseTypeChunkMerger("Person");

            // Act
            merger.VisitChunk(chunk);

            // Assert
            Assert.Equal(expectedValue, chunk.TypeName);
        }

        [Fact]
        public void Merge_ThrowsIfThePassedInChunkIsNotASetBaseTypeChunk()
        {
            // Arrange
            var expected = "Argument must be an instance of " +
                           "'Microsoft.AspNet.Razor.Chunks.SetBaseTypeChunk'.";
            var merger = new SetBaseTypeChunkMerger("dynamic");

            // Act and Assert
            ExceptionAssert.ThrowsArgument(() => merger.Merge(new ChunkTree(), new LiteralChunk()), "chunk", expected);
        }

        [Fact]
        public void Merge_SetsBaseTypeIfItHasNotBeenSetInChunkTree()
        {
            // Arrange
            var expected = "MyApp.Razor.MyBaseType";
            var merger = new SetBaseTypeChunkMerger("dynamic");
            var chunkTree = new ChunkTree();

            // Act
            merger.Merge(chunkTree, new SetBaseTypeChunk { TypeName = expected });

            // Assert
            var chunk = Assert.Single(chunkTree.Chunks);
            var setBaseTypeChunk = Assert.IsType<SetBaseTypeChunk>(chunk);
            Assert.Equal(expected, setBaseTypeChunk.TypeName);
        }

        [Fact]
        public void Merge_IgnoresSetBaseTypeChunksIfChunkTreeContainsOne()
        {
            // Arrange
            var merger = new SetBaseTypeChunkMerger("dynamic");
            var chunkTree = new ChunkTree();

            // Act
            merger.VisitChunk(new SetBaseTypeChunk { TypeName = "MyBaseType1" });
            merger.Merge(chunkTree, new SetBaseTypeChunk { TypeName = "MyBaseType2" });

            // Assert
            Assert.Empty(chunkTree.Chunks);
        }

        [Fact]
        public void Merge_IgnoresSetBaseTypeChunksIfSetBaseTypeWasPreviouslyMerged()
        {
            // Arrange
            var merger = new SetBaseTypeChunkMerger("dynamic");
            var chunkTree = new ChunkTree();

            // Act
            merger.Merge(chunkTree, new SetBaseTypeChunk { TypeName = "MyBase1" });
            merger.Merge(chunkTree, new SetBaseTypeChunk { TypeName = "MyBase2" });

            // Assert
            var chunk = Assert.Single(chunkTree.Chunks);
            var setBaseTypeChunk = Assert.IsType<SetBaseTypeChunk>(chunk);
            Assert.Equal("MyBase1", setBaseTypeChunk.TypeName);
        }
    }
}