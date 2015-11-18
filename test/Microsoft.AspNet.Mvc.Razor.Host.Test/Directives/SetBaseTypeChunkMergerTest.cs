// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Chunks;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor.Directives
{
    public class SetBaseTypeChunkMergerTest
    {
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
        public void Merge_SetsBaseTypeIfItHasNotBeenSetInChunkTree()
        {
            // Arrange
            var expected = "MyApp.Razor.MyBaseType";
            var merger = new SetBaseTypeChunkMerger("dynamic");
            var chunkTree = new ChunkTree();
            var inheritedChunks = new[]
            {
                new SetBaseTypeChunk { TypeName = expected }
            };

            // Act
            merger.MergeInheritedChunks(chunkTree, inheritedChunks);

            // Assert
            var chunk = Assert.Single(chunkTree.Children);
            var setBaseTypeChunk = Assert.IsType<SetBaseTypeChunk>(chunk);
            Assert.Equal(expected, setBaseTypeChunk.TypeName);
        }

        [Fact]
        public void Merge_IgnoresSetBaseTypeChunksIfChunkTreeContainsOne()
        {
            // Arrange
            var merger = new SetBaseTypeChunkMerger("dynamic");
            var chunkTree = new ChunkTree();
            var inheritedChunks = new[]
            {
                 new SetBaseTypeChunk { TypeName = "MyBaseType2" }
            };

            // Act
            merger.VisitChunk(new SetBaseTypeChunk { TypeName = "MyBaseType1" });
            merger.MergeInheritedChunks(chunkTree, inheritedChunks);

            // Assert
            Assert.Empty(chunkTree.Children);
        }

        [Fact]
        public void Merge_PicksLastBaseTypeChunkFromChunkTree()
        {
            // Arrange
            var merger = new SetBaseTypeChunkMerger("dynamic");
            var chunkTree = new ChunkTree();
            var inheritedChunks = new Chunk[]
            {
                 new SetBaseTypeChunk { TypeName = "MyBase2" },
                 new LiteralChunk(),
                 new SetBaseTypeChunk { TypeName = "MyBase1" },
            };

            // Act
            merger.MergeInheritedChunks(chunkTree, inheritedChunks);

            // Assert
            var chunk = Assert.Single(chunkTree.Children);
            var setBaseTypeChunk = Assert.IsType<SetBaseTypeChunk>(chunk);
            Assert.Equal("MyBase1", setBaseTypeChunk.TypeName);
        }
    }
}