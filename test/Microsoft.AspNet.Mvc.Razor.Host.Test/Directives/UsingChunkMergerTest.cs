// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Chunks;
using Microsoft.AspNet.Testing;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor.Directives
{
    public class UsingChunkMergerTest
    {
        [Fact]
        public void Visit_ThrowsIfThePassedInChunkIsNotAUsingChunk()
        {
            // Arrange
            var expected = "Argument must be an instance of 'Microsoft.AspNet.Razor.Chunks.UsingChunk'.";
            var merger = new UsingChunkMerger();

            // Act and Assert
            ExceptionAssert.ThrowsArgument(() => merger.VisitChunk(new LiteralChunk()), "chunk", expected);
        }

        [Fact]
        public void Merge_ThrowsIfThePassedInChunkIsNotAUsingChunk()
        {
            // Arrange
            var expected = "Argument must be an instance of 'Microsoft.AspNet.Razor.Chunks.UsingChunk'.";
            var merger = new UsingChunkMerger();

            // Act and Assert
            ExceptionAssert.ThrowsArgument(() => merger.Merge(new ChunkTree(), new LiteralChunk()), "chunk", expected);
        }

        [Fact]
        public void Merge_AddsNamespacesThatHaveNotBeenVisitedInChunkTree()
        {
            // Arrange
            var expected = "MyApp.Models";
            var merger = new UsingChunkMerger();
            var chunkTree = new ChunkTree();

            // Act
            merger.VisitChunk(new UsingChunk { Namespace = "Microsoft.AspNet.Mvc" });
            merger.Merge(chunkTree, new UsingChunk { Namespace = expected });

            // Assert
            var chunk = Assert.Single(chunkTree.Chunks);
            var usingChunk = Assert.IsType<UsingChunk>(chunk);
            Assert.Equal(expected, usingChunk.Namespace);
        }

        [Fact]
        public void Merge_IgnoresNamespacesThatHaveBeenVisitedInChunkTree()
        {
            // Arrange
            var merger = new UsingChunkMerger();
            var chunkTree = new ChunkTree();

            // Act
            merger.VisitChunk(new UsingChunk { Namespace = "Microsoft.AspNet.Mvc" });
            merger.Merge(chunkTree, new UsingChunk { Namespace = "Microsoft.AspNet.Mvc" });

            // Assert
            Assert.Empty(chunkTree.Chunks);
        }

        [Fact]
        public void Merge_IgnoresNamespacesThatHaveBeenVisitedDuringMerge()
        {
            // Arrange
            var merger = new UsingChunkMerger();
            var chunkTree = new ChunkTree();

            // Act
            merger.Merge(chunkTree, new UsingChunk { Namespace = "Microsoft.AspNet.Mvc" });
            merger.Merge(chunkTree, new UsingChunk { Namespace = "Microsoft.AspNet.Mvc" });
            merger.Merge(chunkTree, new UsingChunk { Namespace = "Microsoft.AspNet.Mvc.Razor" });

            // Assert
            Assert.Equal(2, chunkTree.Chunks.Count);
            var chunk = Assert.IsType<UsingChunk>(chunkTree.Chunks[0]);
            Assert.Equal("Microsoft.AspNet.Mvc", chunk.Namespace);
            chunk = Assert.IsType<UsingChunk>(chunkTree.Chunks[1]);
            Assert.Equal("Microsoft.AspNet.Mvc.Razor", chunk.Namespace);
        }

        [Fact]
        public void Merge_MacthesNamespacesInCaseSensitiveManner()
        {
            // Arrange
            var merger = new UsingChunkMerger();
            var chunkTree = new ChunkTree();

            // Act
            merger.Merge(chunkTree, new UsingChunk { Namespace = "Microsoft.AspNet.Mvc" });
            merger.Merge(chunkTree, new UsingChunk { Namespace = "Microsoft.AspNet.mvc" });

            // Assert
            Assert.Equal(2, chunkTree.Chunks.Count);
            var chunk = Assert.IsType<UsingChunk>(chunkTree.Chunks[0]);
            Assert.Equal("Microsoft.AspNet.Mvc", chunk.Namespace);
            chunk = Assert.IsType<UsingChunk>(chunkTree.Chunks[1]);
            Assert.Equal("Microsoft.AspNet.mvc", chunk.Namespace);
        }
    }
}