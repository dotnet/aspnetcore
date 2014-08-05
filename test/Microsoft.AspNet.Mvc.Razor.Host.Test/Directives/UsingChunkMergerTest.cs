// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Generator.Compiler;
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
            var expected = "Argument must be an instance of 'Microsoft.AspNet.Razor.Generator.Compiler.UsingChunk'.";
            var merger = new UsingChunkMerger();

            // Act and Assert
            ExceptionAssert.ThrowsArgument(() => merger.VisitChunk(new LiteralChunk()), "chunk", expected);
        }

        [Fact]
        public void Merge_ThrowsIfThePassedInChunkIsNotAUsingChunk()
        {
            // Arrange
            var expected = "Argument must be an instance of 'Microsoft.AspNet.Razor.Generator.Compiler.UsingChunk'.";
            var merger = new UsingChunkMerger();

            // Act and Assert
            ExceptionAssert.ThrowsArgument(() => merger.Merge(new CodeTree(), new LiteralChunk()), "chunk", expected);
        }

        [Fact]
        public void Merge_AddsNamespacesThatHaveNotBeenVisitedInCodeTree()
        {
            // Arrange
            var expected = "MyApp.Models";
            var merger = new UsingChunkMerger();
            var codeTree = new CodeTree();

            // Act
            merger.VisitChunk(new UsingChunk { Namespace = "Microsoft.AspNet.Mvc" });
            merger.Merge(codeTree, new UsingChunk { Namespace = expected });

            // Assert
            var chunk = Assert.Single(codeTree.Chunks);
            var usingChunk = Assert.IsType<UsingChunk>(chunk);
            Assert.Equal(expected, usingChunk.Namespace);
        }

        [Fact]
        public void Merge_IgnoresNamespacesThatHaveBeenVisitedInCodeTree()
        {
            // Arrange
            var merger = new UsingChunkMerger();
            var codeTree = new CodeTree();

            // Act
            merger.VisitChunk(new UsingChunk { Namespace = "Microsoft.AspNet.Mvc" });
            merger.Merge(codeTree, new UsingChunk { Namespace = "Microsoft.AspNet.Mvc" });

            // Assert
            Assert.Empty(codeTree.Chunks);
        }

        [Fact]
        public void Merge_IgnoresNamespacesThatHaveBeenVisitedDuringMerge()
        {
            // Arrange
            var merger = new UsingChunkMerger();
            var codeTree = new CodeTree();

            // Act
            merger.Merge(codeTree, new UsingChunk { Namespace = "Microsoft.AspNet.Mvc" });
            merger.Merge(codeTree, new UsingChunk { Namespace = "Microsoft.AspNet.Mvc" });
            merger.Merge(codeTree, new UsingChunk { Namespace = "Microsoft.AspNet.Mvc.Razor" });

            // Assert
            Assert.Equal(2, codeTree.Chunks.Count);
            var chunk = Assert.IsType<UsingChunk>(codeTree.Chunks[0]);
            Assert.Equal("Microsoft.AspNet.Mvc", chunk.Namespace);
            chunk = Assert.IsType<UsingChunk>(codeTree.Chunks[1]);
            Assert.Equal("Microsoft.AspNet.Mvc.Razor", chunk.Namespace);
        }

        [Fact]
        public void Merge_MacthesNamespacesInCaseSensitiveManner()
        {
            // Arrange
            var merger = new UsingChunkMerger();
            var codeTree = new CodeTree();

            // Act
            merger.Merge(codeTree, new UsingChunk { Namespace = "Microsoft.AspNet.Mvc" });
            merger.Merge(codeTree, new UsingChunk { Namespace = "Microsoft.AspNet.mvc" });

            // Assert
            Assert.Equal(2, codeTree.Chunks.Count);
            var chunk = Assert.IsType<UsingChunk>(codeTree.Chunks[0]);
            Assert.Equal("Microsoft.AspNet.Mvc", chunk.Namespace);
            chunk = Assert.IsType<UsingChunk>(codeTree.Chunks[1]);
            Assert.Equal("Microsoft.AspNet.mvc", chunk.Namespace);
        }
    }
}