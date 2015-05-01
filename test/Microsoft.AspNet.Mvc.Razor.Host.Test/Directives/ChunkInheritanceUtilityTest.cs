// Copyright (c) .NET Foundation. All rights reserved.
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
            Assert.Collection(codeTrees,
                codeTree =>
                {
                    var globalImportPath = @"Views\home\_GlobalImport.cshtml";
                    Assert.Collection(codeTree.Chunks,
                        chunk =>
                        {
                            Assert.IsType<LiteralChunk>(chunk);
                            Assert.Equal(globalImportPath, chunk.Start.FilePath);
                        },
                        chunk =>
                        {
                            var usingChunk = Assert.IsType<UsingChunk>(chunk);
                            Assert.Equal("MyNamespace", usingChunk.Namespace);
                            Assert.Equal(globalImportPath, chunk.Start.FilePath);
                        },
                        chunk =>
                        {
                            Assert.IsType<LiteralChunk>(chunk);
                            Assert.Equal(globalImportPath, chunk.Start.FilePath);
                        });
                },
                codeTree =>
                {
                    var globalImportPath = @"Views\_GlobalImport.cshtml";
                    Assert.Collection(codeTree.Chunks,
                        chunk =>
                        {
                            Assert.IsType<LiteralChunk>(chunk);
                            Assert.Equal(globalImportPath, chunk.Start.FilePath);
                        },
                        chunk =>
                        {
                            var injectChunk = Assert.IsType<InjectChunk>(chunk);
                            Assert.Equal("MyHelper<TModel>", injectChunk.TypeName);
                            Assert.Equal("Helper", injectChunk.MemberName);
                            Assert.Equal(globalImportPath, chunk.Start.FilePath);
                        },
                        chunk =>
                        {
                            Assert.IsType<LiteralChunk>(chunk);
                            Assert.Equal(globalImportPath, chunk.Start.FilePath);
                        },
                        chunk =>
                        {
                            var setBaseTypeChunk = Assert.IsType<SetBaseTypeChunk>(chunk);
                            Assert.Equal("MyBaseType", setBaseTypeChunk.TypeName);
                            Assert.Equal(globalImportPath, chunk.Start.FilePath);

                        },
                        chunk =>
                        {
                            Assert.IsType<LiteralChunk>(chunk);
                            Assert.Equal(globalImportPath, chunk.Start.FilePath);
                        },
                        chunk =>
                        {
                            Assert.IsType<StatementChunk>(chunk);
                            Assert.Equal(globalImportPath, chunk.Start.FilePath);
                        },
                        chunk =>
                        {
                            Assert.IsType<LiteralChunk>(chunk);
                            Assert.Equal(globalImportPath, chunk.Start.FilePath);
                        });
                });
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