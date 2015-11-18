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
        public void Merge_AddsNamespacesThatHaveNotBeenVisitedInChunkTree()
        {
            // Arrange
            var expected = "MyApp.Models";
            var merger = new UsingChunkMerger();
            var chunkTree = new ChunkTree();
            var inheritedChunks = new Chunk[]
            {
                new UsingChunk { Namespace = expected },
            };

            // Act
            merger.VisitChunk(new UsingChunk { Namespace = "Microsoft.AspNet.Mvc" });
            merger.MergeInheritedChunks(chunkTree, inheritedChunks);

            // Assert
            var chunk = Assert.Single(chunkTree.Children);
            var usingChunk = Assert.IsType<UsingChunk>(chunk);
            Assert.Equal(expected, usingChunk.Namespace);
        }

        [Fact]
        public void Merge_IgnoresNamespacesThatHaveBeenVisitedInChunkTree()
        {
            // Arrange
            var merger = new UsingChunkMerger();
            var chunkTree = new ChunkTree();
            var inheritedChunks = new Chunk[]
            {
                new UsingChunk { Namespace = "Microsoft.AspNet.Mvc" },
                new InjectChunk("Foo", "Bar")
            };

            // Act
            merger.VisitChunk(new UsingChunk { Namespace = "Microsoft.AspNet.Mvc" });
            merger.MergeInheritedChunks(chunkTree, inheritedChunks);

            // Assert
            Assert.Empty(chunkTree.Children);
        }

        [Fact]
        public void Merge_DoesNotAddMoreThanOneInstanceOfTheSameInheritedNamespace()
        {
            // Arrange
            var merger = new UsingChunkMerger();
            var chunkTree = new ChunkTree();
            var inheritedChunks = new Chunk[]
            {
                new LiteralChunk(),
                new UsingChunk { Namespace = "Microsoft.AspNet.Mvc" },
                new UsingChunk { Namespace = "Microsoft.AspNet.Mvc" },
                new UsingChunk { Namespace = "Microsoft.AspNet.Mvc.Razor" }
            };

            // Act
            merger.MergeInheritedChunks(chunkTree, inheritedChunks);

            // Assert
            Assert.Equal(2, chunkTree.Children.Count);
            var chunk = Assert.IsType<UsingChunk>(chunkTree.Children[0]);
            Assert.Equal("Microsoft.AspNet.Mvc", chunk.Namespace);
            chunk = Assert.IsType<UsingChunk>(chunkTree.Children[1]);
            Assert.Equal("Microsoft.AspNet.Mvc.Razor", chunk.Namespace);
        }

        [Fact]
        public void Merge_MatchesNamespacesInCaseSensitiveManner()
        {
            // Arrange
            var merger = new UsingChunkMerger();
            var chunkTree = new ChunkTree();
            var inheritedChunks = new[]
            {
                new UsingChunk { Namespace = "Microsoft.AspNet.Mvc" },
                new UsingChunk { Namespace = "Microsoft.AspNet.mvc" }
            };

            // Act
            merger.MergeInheritedChunks(chunkTree, inheritedChunks);

            // Assert
            Assert.Equal(2, chunkTree.Children.Count);
            var chunk = Assert.IsType<UsingChunk>(chunkTree.Children[0]);
            Assert.Equal("Microsoft.AspNet.Mvc", chunk.Namespace);
            chunk = Assert.IsType<UsingChunk>(chunkTree.Children[1]);
            Assert.Equal("Microsoft.AspNet.mvc", chunk.Namespace);
        }
    }
}