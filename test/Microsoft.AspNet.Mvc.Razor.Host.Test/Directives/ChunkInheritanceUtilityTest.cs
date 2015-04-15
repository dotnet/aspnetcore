// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Generator.Compiler;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor.Directives
{
    public class ChunkInheritanceUtilityTest
    {
        [Fact]
        public void GetInheritedChunks_ReadsChunksFromGlobalFilesInPath()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            fileProvider.AddFile(@"Views\accounts\_GlobalImport.cshtml", "@using AccountModels");
            fileProvider.AddFile(@"Views\Shared\_GlobalImport.cshtml", "@inject SharedHelper Shared");
            fileProvider.AddFile(@"Views\home\_GlobalImport.cshtml", "@using MyNamespace");
            fileProvider.AddFile(@"Views\_GlobalImport.cshtml",
@"@inject MyHelper<TModel> Helper
@inherits MyBaseType

@{
    Layout = ""test.cshtml"";
}

");
            var defaultChunks = new Chunk[]
            {
                new InjectChunk("MyTestHtmlHelper", "Html"),
                new UsingChunk { Namespace = "AppNamespace.Model" },
            };
            var cache = new DefaultCodeTreeCache(fileProvider);
            var host = new MvcRazorHost(cache);
            var utility = new ChunkInheritanceUtility(host, cache, defaultChunks);

            // Act
            var codeTrees = utility.GetInheritedCodeTrees(@"Views\home\Index.cshtml");

            // Assert
            Assert.Equal(2, codeTrees.Count);
            var viewStartChunks = codeTrees[0].Chunks;
            Assert.Equal(3, viewStartChunks.Count);

            Assert.IsType<LiteralChunk>(viewStartChunks[0]);
            var usingChunk = Assert.IsType<UsingChunk>(viewStartChunks[1]);
            Assert.Equal("MyNamespace", usingChunk.Namespace);
            Assert.IsType<LiteralChunk>(viewStartChunks[2]);

            viewStartChunks = codeTrees[1].Chunks;
            Assert.Equal(7, viewStartChunks.Count);

            Assert.IsType<LiteralChunk>(viewStartChunks[0]);

            var injectChunk = Assert.IsType<InjectChunk>(viewStartChunks[1]);
            Assert.Equal("MyHelper<TModel>", injectChunk.TypeName);
            Assert.Equal("Helper", injectChunk.MemberName);

            Assert.IsType<LiteralChunk>(viewStartChunks[2]);

            var setBaseTypeChunk = Assert.IsType<SetBaseTypeChunk>(viewStartChunks[3]);
            Assert.Equal("MyBaseType", setBaseTypeChunk.TypeName);

            Assert.IsType<LiteralChunk>(viewStartChunks[4]);

            Assert.IsType<StatementChunk>(viewStartChunks[5]);
            Assert.IsType<LiteralChunk>(viewStartChunks[6]);
        }

        [Fact]
        public void GetInheritedChunks_ReturnsEmptySequenceIfNoGlobalsArePresent()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            fileProvider.AddFile(@"_GlobalImport.cs", string.Empty);
            fileProvider.AddFile(@"Views\_Layout.cshtml", string.Empty);
            fileProvider.AddFile(@"Views\home\_not-globalimport.cshtml", string.Empty);
            var cache = new DefaultCodeTreeCache(fileProvider);
            var host = new MvcRazorHost(cache);
            var defaultChunks = new Chunk[]
            {
                new InjectChunk("MyTestHtmlHelper", "Html"),
                new UsingChunk { Namespace = "AppNamespace.Model" },
            };
            var utility = new ChunkInheritanceUtility(host, cache, defaultChunks);

            // Act
            var codeTrees = utility.GetInheritedCodeTrees(@"Views\home\Index.cshtml");

            // Assert
            Assert.Empty(codeTrees);
        }

        [Fact]
        public void MergeInheritedChunks_MergesDefaultInheritedChunks()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            fileProvider.AddFile(@"Views\_GlobalImport.cshtml",
                               "@inject DifferentHelper<TModel> Html");
            var cache = new DefaultCodeTreeCache(fileProvider);
            var host = new MvcRazorHost(cache);
            var defaultChunks = new Chunk[]
            {
                new InjectChunk("MyTestHtmlHelper", "Html"),
                new UsingChunk { Namespace = "AppNamespace.Model" },
            };
            var inheritedCodeTrees = new CodeTree[]
            {
                new CodeTree
                {
                    Chunks = new Chunk[]
                    {
                        new UsingChunk { Namespace = "InheritedNamespace" },
                        new LiteralChunk { Text = "some text" }
                    }
                },
                new CodeTree
                {
                    Chunks = new Chunk[]
                    {
                        new UsingChunk { Namespace = "AppNamespace.Model" },
                    }
                }
            };

            var utility = new ChunkInheritanceUtility(host, cache, defaultChunks);
            var codeTree = new CodeTree();

            // Act
            utility.MergeInheritedCodeTrees(codeTree,
                                            inheritedCodeTrees,
                                            "dynamic");

            // Assert
            Assert.Equal(3, codeTree.Chunks.Count);
            Assert.Same(inheritedCodeTrees[0].Chunks[0], codeTree.Chunks[0]);
            Assert.Same(inheritedCodeTrees[1].Chunks[0], codeTree.Chunks[1]);
            Assert.Same(defaultChunks[0], codeTree.Chunks[2]);
        }
    }
}