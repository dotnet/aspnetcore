// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Chunks;
using Microsoft.AspNet.Testing;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor.Directives
{
    public class InjectChunkMergerTest
    {
        [Fact]
        public void Visit_ThrowsIfThePassedInChunkIsNotAInjectChunk()
        {
            // Arrange
            var expected = "Argument must be an instance of 'Microsoft.AspNet.Mvc.Razor.InjectChunk'.";
            var merger = new InjectChunkMerger("dynamic");

            // Act and Assert
            ExceptionAssert.ThrowsArgument(() => merger.VisitChunk(new LiteralChunk()), "chunk", expected);
        }

        [Theory]
        [InlineData("MyApp.TestHelper<TModel>", "MyApp.TestHelper<Person>")]
        [InlineData("TestBaseType", "TestBaseType")]
        public void Visit_UpdatesTModelTokenToMatchModelType(string typeName, string expectedValue)
        {
            // Arrange
            var chunk = new InjectChunk(typeName, "TestHelper");
            var merger = new InjectChunkMerger("Person");

            // Act
            merger.VisitChunk(chunk);

            // Assert
            Assert.Equal(expectedValue, chunk.TypeName);
            Assert.Equal("TestHelper", chunk.MemberName);
        }

        [Fact]
        public void Merge_ThrowsIfThePassedInChunkIsNotAInjectChunk()
        {
            // Arrange
            var expected = "Argument must be an instance of 'Microsoft.AspNet.Mvc.Razor.InjectChunk'.";
            var merger = new InjectChunkMerger("dynamic");

            // Act and Assert
            ExceptionAssert.ThrowsArgument(() => merger.Merge(new ChunkTree(), new LiteralChunk()), "chunk", expected);
        }

        [Fact]
        public void Merge_AddsChunkIfChunkWithMatchingPropertyNameWasNotVisitedInChunkTree()
        {
            // Arrange
            var expectedType = "MyApp.MyHelperType";
            var expectedProperty = "MyHelper";
            var merger = new InjectChunkMerger("dynamic");
            var chunkTree = new ChunkTree();

            // Act
            merger.Merge(chunkTree, new InjectChunk(expectedType, expectedProperty));

            // Assert
            var chunk = Assert.Single(chunkTree.Chunks);
            var injectChunk = Assert.IsType<InjectChunk>(chunk);
            Assert.Equal(expectedType, injectChunk.TypeName);
            Assert.Equal(expectedProperty, injectChunk.MemberName);
        }

        [Fact]
        public void Merge_IgnoresChunkIfChunkWithMatchingPropertyNameWasVisitedInChunkTree()
        {
            // Arrange
            var merger = new InjectChunkMerger("dynamic");
            var chunkTree = new ChunkTree();

            // Act
            merger.VisitChunk(new InjectChunk("MyTypeA", "MyProperty"));
            merger.Merge(chunkTree, new InjectChunk("MyTypeB", "MyProperty"));

            // Assert
            Assert.Empty(chunkTree.Chunks);
        }

        [Fact]
        public void Merge_MatchesPropertyNameInCaseSensitiveManner()
        {
            // Arrange
            var merger = new InjectChunkMerger("dynamic");
            var chunkTree = new ChunkTree();

            // Act
            merger.VisitChunk(new InjectChunk("MyType", "MyProperty"));
            merger.Merge(chunkTree, new InjectChunk("MyType", "myproperty"));
            merger.Merge(chunkTree, new InjectChunk("MyTypeB", "different-property"));

            // Assert
            Assert.Equal(2, chunkTree.Chunks.Count);
            var injectChunk = Assert.IsType<InjectChunk>(chunkTree.Chunks[0]);
            Assert.Equal("MyType", injectChunk.TypeName);
            Assert.Equal("myproperty", injectChunk.MemberName);

            injectChunk = Assert.IsType<InjectChunk>(chunkTree.Chunks[1]);
            Assert.Equal("MyTypeB", injectChunk.TypeName);
            Assert.Equal("different-property", injectChunk.MemberName);
        }

        [Fact]
        public void Merge_ResolvesModelNameInTypesWithTModelToken()
        {
            // Arrange
            var merger = new InjectChunkMerger("dynamic");
            var chunkTree = new ChunkTree();

            // Act
            merger.Merge(chunkTree, new InjectChunk("MyHelper<TModel>", "MyProperty"));

            // Assert
            var chunk = Assert.Single(chunkTree.Chunks);
            var injectChunk = Assert.IsType<InjectChunk>(chunk);
            Assert.Equal("MyHelper<dynamic>", injectChunk.TypeName);
            Assert.Equal("MyProperty", injectChunk.MemberName);
        }

        [Fact]
        public void Merge_ReplacesTModelTokensWithModel()
        {
            // Arrange
            var merger = new InjectChunkMerger("MyTestModel2");
            var chunkTree = new ChunkTree();

            // Act
            merger.Merge(chunkTree, new InjectChunk("MyHelper<TModel>", "MyProperty"));

            // Assert
            var chunk = Assert.Single(chunkTree.Chunks);
            var injectChunk = Assert.IsType<InjectChunk>(chunk);
            Assert.Equal("MyHelper<MyTestModel2>", injectChunk.TypeName);
            Assert.Equal("MyProperty", injectChunk.MemberName);
        }

        [Fact]
        public void Merge_IgnoresChunkIfChunkWithMatchingPropertyNameWasPreviouslyMerged()
        {
            // Arrange
            var merger = new InjectChunkMerger("dynamic");
            var chunkTree = new ChunkTree();

            // Act
            merger.Merge(chunkTree, new InjectChunk("SomeType", "Property"));
            merger.Merge(chunkTree, new InjectChunk("SomeOtherType", "Property"));

            // Assert
            var chunk = Assert.Single(chunkTree.Chunks);
            var injectChunk = Assert.IsType<InjectChunk>(chunk);
            Assert.Equal("SomeType", injectChunk.TypeName);
            Assert.Equal("Property", injectChunk.MemberName);
        }
    }
}