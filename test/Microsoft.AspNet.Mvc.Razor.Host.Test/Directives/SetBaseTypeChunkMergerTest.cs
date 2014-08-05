// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Generator.Compiler;
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
            var expected = "Argument must be an instance of "+
                           "'Microsoft.AspNet.Razor.Generator.Compiler.SetBaseTypeChunk'.";
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
                           "'Microsoft.AspNet.Razor.Generator.Compiler.SetBaseTypeChunk'.";
            var merger = new SetBaseTypeChunkMerger("dynamic");

            // Act and Assert
            ExceptionAssert.ThrowsArgument(() => merger.Merge(new CodeTree(), new LiteralChunk()), "chunk", expected);
        }

        [Fact]
        public void Merge_SetsBaseTypeIfItHasNotBeenSetInCodeTree()
        {
            // Arrange
            var expected = "MyApp.Razor.MyBaseType";
            var merger = new SetBaseTypeChunkMerger("dynamic");
            var codeTree = new CodeTree();

            // Act
            merger.Merge(codeTree, new SetBaseTypeChunk { TypeName = expected });

            // Assert
            var chunk = Assert.Single(codeTree.Chunks);
            var setBaseTypeChunk = Assert.IsType<SetBaseTypeChunk>(chunk);
            Assert.Equal(expected, setBaseTypeChunk.TypeName);
        }

        [Fact]
        public void Merge_IgnoresSetBaseTypeChunksIfCodeTreeContainsOne()
        {
            // Arrange
            var merger = new SetBaseTypeChunkMerger("dynamic");
            var codeTree = new CodeTree();

            // Act
            merger.VisitChunk(new SetBaseTypeChunk { TypeName = "MyBaseType1" });
            merger.Merge(codeTree, new SetBaseTypeChunk { TypeName = "MyBaseType2" });

            // Assert
            Assert.Empty(codeTree.Chunks);
        }

        [Fact]
        public void Merge_IgnoresSetBaseTypeChunksIfSetBaseTypeWasPreviouslyMerged()
        {
            // Arrange
            var merger = new SetBaseTypeChunkMerger("dynamic");
            var codeTree = new CodeTree();

            // Act
            merger.Merge(codeTree, new SetBaseTypeChunk { TypeName = "MyBase1" });
            merger.Merge(codeTree, new SetBaseTypeChunk { TypeName = "MyBase2" });

            // Assert
            var chunk = Assert.Single(codeTree.Chunks);
            var setBaseTypeChunk = Assert.IsType<SetBaseTypeChunk>(chunk);
            Assert.Equal("MyBase1", setBaseTypeChunk.TypeName);
        }
    }
}