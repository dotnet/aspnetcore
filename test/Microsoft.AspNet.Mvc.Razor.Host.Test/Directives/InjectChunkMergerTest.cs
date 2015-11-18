// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Chunks;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor.Directives
{
    public class InjectChunkMergerTest
    {
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
        public void Merge_AddsChunkIfChunkWithMatchingPropertyNameWasNotVisitedInChunkTree()
        {
            // Arrange
            var expectedType = "MyApp.MyHelperType";
            var expectedProperty = "MyHelper";
            var merger = new InjectChunkMerger("dynamic");
            var chunkTree = new ChunkTree();
            var inheritedChunks = new[]
            {
                new InjectChunk(expectedType, expectedProperty)
            };

            // Act
            merger.MergeInheritedChunks(chunkTree, inheritedChunks);

            // Assert
            var chunk = Assert.Single(chunkTree.Children);
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
            var inheritedChunks = new[]
            {
                new InjectChunk("MyTypeB", "MyProperty")
            };

            // Act
            merger.VisitChunk(new InjectChunk("MyTypeA", "MyProperty"));
            merger.MergeInheritedChunks(chunkTree, inheritedChunks);

            // Assert
            Assert.Empty(chunkTree.Children);
        }

        [Fact]
        public void Merge_MatchesPropertyNameInCaseSensitiveManner()
        {
            // Arrange
            var merger = new InjectChunkMerger("dynamic");
            var chunkTree = new ChunkTree();
            var inheritedChunks = new[]
            {
                new InjectChunk("MyTypeB", "different-property"),
                new InjectChunk("MyType", "myproperty"),
            };

            // Act
            merger.VisitChunk(new InjectChunk("MyType", "MyProperty"));
            merger.MergeInheritedChunks(chunkTree, inheritedChunks);

            // Assert
            Assert.Equal(2, chunkTree.Children.Count);
            var injectChunk = Assert.IsType<InjectChunk>(chunkTree.Children[0]);
            Assert.Equal("MyType", injectChunk.TypeName);
            Assert.Equal("myproperty", injectChunk.MemberName);

            injectChunk = Assert.IsType<InjectChunk>(chunkTree.Children[1]);
            Assert.Equal("MyTypeB", injectChunk.TypeName);
            Assert.Equal("different-property", injectChunk.MemberName);
        }

        [Fact]
        public void Merge_ResolvesModelNameInTypesWithTModelToken()
        {
            // Arrange
            var merger = new InjectChunkMerger("dynamic");
            var chunkTree = new ChunkTree();
            var inheritedChunks = new[]
            {
                new InjectChunk("MyHelper<TModel>", "MyProperty")
            };

            // Act
            merger.MergeInheritedChunks(chunkTree, inheritedChunks);

            // Assert
            var chunk = Assert.Single(chunkTree.Children);
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
            var inheritedChunks = new[]
            {
                new InjectChunk("MyHelper<TModel>", "MyProperty")
            };

            // Act
            merger.MergeInheritedChunks(chunkTree, inheritedChunks);

            // Assert
            var chunk = Assert.Single(chunkTree.Children);
            var injectChunk = Assert.IsType<InjectChunk>(chunk);
            Assert.Equal("MyHelper<MyTestModel2>", injectChunk.TypeName);
            Assert.Equal("MyProperty", injectChunk.MemberName);
        }

        [Fact]
        public void Merge_UsesTheLastInjectChunkOfAPropertyName()
        {
            // Arrange
            var merger = new InjectChunkMerger("dynamic");
            var chunkTree = new ChunkTree();
            var inheritedChunks = new Chunk[]
            {
                new LiteralChunk(),
                new InjectChunk("SomeOtherType", "Property"),
                new InjectChunk("DifferentPropertyType", "DifferentProperty"),
                new InjectChunk("SomeType", "Property"),
            };

            // Act
            merger.MergeInheritedChunks(chunkTree, inheritedChunks);

            // Assert
            Assert.Collection(chunkTree.Children,
                chunk =>
                {
                    var injectChunk = Assert.IsType<InjectChunk>(chunk);
                    Assert.Equal("SomeType", injectChunk.TypeName);
                    Assert.Equal("Property", injectChunk.MemberName);
                },
                chunk =>
                {
                    var injectChunk = Assert.IsType<InjectChunk>(chunk);
                    Assert.Equal("DifferentPropertyType", injectChunk.TypeName);
                    Assert.Equal("DifferentProperty", injectChunk.MemberName);
                });
        }
    }
}