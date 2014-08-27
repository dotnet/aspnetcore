// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor.Directives
{
    public class ChunkInheritanceUtilityTest
    {
        [Fact]
        public void GetInheritedChunks_ReadsChunksFromViewStartsInPath()
        {
            // Arrange
            var fileSystem = new TestFileSystem();
            fileSystem.AddFile(@"x:\myapproot\views\accounts\_viewstart.cshtml", "@using AccountModels");
            fileSystem.AddFile(@"x:\myapproot\views\Shared\_viewstart.cshtml", "@inject SharedHelper Shared");
            fileSystem.AddFile(@"x:\myapproot\views\home\_viewstart.cshtml", "@using MyNamespace");
            fileSystem.AddFile(@"x:\myapproot\views\_viewstart.cshtml",
@"@inject MyHelper<TModel> Helper
@inherits MyBaseType

@{
    Layout = ""test.cshtml"";
}

");
            var host = new MvcRazorHost(fileSystem);
            var utility = new ChunkInheritanceUtility(new CodeTree(), new Chunk[0], "dynamic");

            // Act
            var chunks = utility.GetInheritedChunks(host,
                                                    fileSystem,
                                                    @"x:\myapproot\views\home\Index.cshtml");

            // Assert
            Assert.Equal(3, chunks.Count);
            var usingChunk = Assert.IsType<UsingChunk>(chunks[0]);
            Assert.Equal("MyNamespace", usingChunk.Namespace);

            var injectChunk = Assert.IsType<InjectChunk>(chunks[1]);
            Assert.Equal("MyHelper<TModel>", injectChunk.TypeName);
            Assert.Equal("Helper", injectChunk.MemberName);

            var setBaseTypeChunk = Assert.IsType<SetBaseTypeChunk>(chunks[2]);
            Assert.Equal("MyBaseType", setBaseTypeChunk.TypeName);
        }

        [Fact]
        public void GetInheritedChunks_ReturnsEmptySequenceIfNoViewStartsArePresent()
        {
            // Arrange
            var fileSystem = new TestFileSystem();
            fileSystem.AddFile(@"x:\myapproot\_viewstart.cs", string.Empty);
            fileSystem.AddFile(@"x:\myapproot\views\_Layout.cshtml", string.Empty);
            fileSystem.AddFile(@"x:\myapproot\views\home\_not-viewstart.cshtml", string.Empty);
            var host = new MvcRazorHost(fileSystem);
            var utility = new ChunkInheritanceUtility(new CodeTree(), new Chunk[0], "dynamic");

            // Act
            var chunks = utility.GetInheritedChunks(host,
                                                    fileSystem,
                                                    @"x:\myapproot\views\home\Index.cshtml");

            // Assert
            Assert.Empty(chunks);
        }

        [Fact]
        public void GetInheritedChunks_ReturnsDefaultInheritedChunks()
        {
            // Arrange
            var fileSystem = new TestFileSystem();
            fileSystem.AddFile(@"x:\myapproot\views\_viewstart.cshtml",
@"@inject DifferentHelper<TModel> Html
@using AppNamespace.Models
@{
    Layout = ""test.cshtml"";
}

");
            var host = new MvcRazorHost(fileSystem);
            var defaultChunks = new Chunk[]
            {
                new InjectChunk("MyTestHtmlHelper", "Html"),
                new UsingChunk { Namespace = "AppNamespace.Model" },
            };
            var utility = new ChunkInheritanceUtility(new CodeTree(), defaultChunks, "dynamic");

            // Act
            var chunks = utility.GetInheritedChunks(host,
                                                    fileSystem,
                                                    @"x:\myapproot\views\home\Index.cshtml");

            // Assert
            Assert.Equal(4, chunks.Count);
            var injectChunk = Assert.IsType<InjectChunk>(chunks[0]);
            Assert.Equal("DifferentHelper<TModel>", injectChunk.TypeName);
            Assert.Equal("Html", injectChunk.MemberName);

            var usingChunk = Assert.IsType<UsingChunk>(chunks[1]);
            Assert.Equal("AppNamespace.Models", usingChunk.Namespace);

            injectChunk = Assert.IsType<InjectChunk>(chunks[2]);
            Assert.Equal("MyTestHtmlHelper", injectChunk.TypeName);
            Assert.Equal("Html", injectChunk.MemberName);

            usingChunk = Assert.IsType<UsingChunk>(chunks[3]);
            Assert.Equal("AppNamespace.Model", usingChunk.Namespace);
        }

        [Fact]
        public void MergeChunks_VisitsChunksPriorToMerging()
        {
            // Arrange
            var codeTree = new CodeTree();
            codeTree.Chunks.Add(new LiteralChunk());
            codeTree.Chunks.Add(new ExpressionBlockChunk());
            codeTree.Chunks.Add(new ExpressionBlockChunk());
            
            var merger = new Mock<IChunkMerger>();
            var mockSequence = new MockSequence();
            merger.InSequence(mockSequence)
                  .Setup(m => m.VisitChunk(It.IsAny<LiteralChunk>()))
                  .Verifiable();
            merger.InSequence(mockSequence)
                   .Setup(m => m.Merge(codeTree, It.IsAny<LiteralChunk>()))
                   .Verifiable();
            var inheritedChunks = new List<Chunk>
            {
                new CodeAttributeChunk(),
                new LiteralChunk()
            };
            var utility = new ChunkInheritanceUtility(codeTree, inheritedChunks, "dynamic");

            // Act
            utility.ChunkMergers[typeof(LiteralChunk)] = merger.Object;
            utility.MergeInheritedChunks(inheritedChunks);

            // Assert
            merger.Verify();
        }
    }
}