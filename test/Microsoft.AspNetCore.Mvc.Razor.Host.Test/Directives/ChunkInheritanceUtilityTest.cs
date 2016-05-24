// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Chunks;
using Microsoft.AspNetCore.Razor.Runtime.TagHelpers;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Directives
{
    public class ChunkInheritanceUtilityTest
    {
        [Fact]
        public void GetInheritedChunks_ReadsChunksFromGlobalFilesInPath()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            fileProvider.AddFile(@"/Views/accounts/_ViewImports.cshtml", "@using AccountModels");
            fileProvider.AddFile(@"/Views/Shared/_ViewImports.cshtml", "@inject SharedHelper Shared");
            fileProvider.AddFile(@"/Views/home/_ViewImports.cshtml", "@using MyNamespace");
            fileProvider.AddFile(@"/Views/_ViewImports.cshtml",
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
            var cache = new DefaultChunkTreeCache(fileProvider);
            var host = new MvcRazorHost(cache, new TagHelperDescriptorResolver(designTime: false));
            var utility = new ChunkInheritanceUtility(host, cache, defaultChunks);

            // Act
            var chunkTreeResults = utility.GetInheritedChunkTreeResults(
                PlatformNormalizer.NormalizePath(@"Views\home\Index.cshtml"));

            // Assert
            Assert.Collection(chunkTreeResults,
                chunkTreeResult =>
                {
                    var viewImportsPath = @"/Views/_ViewImports.cshtml";
                    Assert.Collection(chunkTreeResult.ChunkTree.Children,
                        chunk =>
                        {
                            Assert.IsType<LiteralChunk>(chunk);
                            Assert.Equal(viewImportsPath, chunk.Start.FilePath);
                        },
                        chunk =>
                        {
                            var injectChunk = Assert.IsType<InjectChunk>(chunk);
                            Assert.Equal("MyHelper<TModel>", injectChunk.TypeName);
                            Assert.Equal("Helper", injectChunk.MemberName);
                            Assert.Equal(viewImportsPath, chunk.Start.FilePath);
                        },
                        chunk =>
                        {
                            Assert.IsType<LiteralChunk>(chunk);
                            Assert.Equal(viewImportsPath, chunk.Start.FilePath);
                        },
                        chunk =>
                        {
                            var setBaseTypeChunk = Assert.IsType<SetBaseTypeChunk>(chunk);
                            Assert.Equal("MyBaseType", setBaseTypeChunk.TypeName);
                            Assert.Equal(viewImportsPath, chunk.Start.FilePath);
                        },
                        chunk =>
                        {
                            Assert.IsType<LiteralChunk>(chunk);
                            Assert.Equal(viewImportsPath, chunk.Start.FilePath);
                        },
                        chunk =>
                        {
                            Assert.IsType<StatementChunk>(chunk);
                            Assert.Equal(viewImportsPath, chunk.Start.FilePath);
                        },
                        chunk =>
                        {
                            Assert.IsType<LiteralChunk>(chunk);
                            Assert.Equal(viewImportsPath, chunk.Start.FilePath);
                        });
                    Assert.Equal(viewImportsPath, chunkTreeResult.FilePath);
                },
                chunkTreeResult =>
                {
                    var viewImportsPath = "/Views/home/_ViewImports.cshtml";
                    Assert.Collection(chunkTreeResult.ChunkTree.Children,
                        chunk =>
                        {
                            Assert.IsType<LiteralChunk>(chunk);
                            Assert.Equal(viewImportsPath, chunk.Start.FilePath);
                        },
                        chunk =>
                        {
                            var usingChunk = Assert.IsType<UsingChunk>(chunk);
                            Assert.Equal("MyNamespace", usingChunk.Namespace);
                            Assert.Equal(viewImportsPath, chunk.Start.FilePath);
                        },
                        chunk =>
                        {
                            Assert.IsType<LiteralChunk>(chunk);
                            Assert.Equal(viewImportsPath, chunk.Start.FilePath);
                        });
                    Assert.Equal(viewImportsPath, chunkTreeResult.FilePath);
                });
        }

        [Fact]
        public void GetInheritedChunks_ReturnsEmptySequenceIfNoGlobalsArePresent()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            fileProvider.AddFile(@"/_ViewImports.cs", string.Empty);
            fileProvider.AddFile(@"/Views/_Layout.cshtml", string.Empty);
            fileProvider.AddFile(@"/Views/home/_not-viewimports.cshtml", string.Empty);
            var cache = new DefaultChunkTreeCache(fileProvider);
            var host = new MvcRazorHost(cache, new TagHelperDescriptorResolver(designTime: false));
            var defaultChunks = new Chunk[]
            {
                new InjectChunk("MyTestHtmlHelper", "Html"),
                new UsingChunk { Namespace = "AppNamespace.Model" },
            };
            var utility = new ChunkInheritanceUtility(host, cache, defaultChunks);

            // Act
            var chunkTrees = utility.GetInheritedChunkTreeResults(PlatformNormalizer.NormalizePath(@"Views\home\Index.cshtml"));

            // Assert
            Assert.Empty(chunkTrees);
        }

        [Fact]
        public void MergeInheritedChunks_MergesDefaultInheritedChunks()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            fileProvider.AddFile(@"/Views/_ViewImports.cshtml",
                               "@inject DifferentHelper<TModel> Html");
            var cache = new DefaultChunkTreeCache(fileProvider);
            var host = new MvcRazorHost(cache, new TagHelperDescriptorResolver(designTime: false));
            var defaultChunks = new Chunk[]
            {
                new InjectChunk("MyTestHtmlHelper", "Html"),
                new UsingChunk { Namespace = "AppNamespace.Model" },
            };
            var inheritedChunkTrees = new ChunkTree[]
            {
                new ChunkTree
                {
                    Children = new Chunk[]
                    {
                        new UsingChunk { Namespace = "InheritedNamespace" },
                        new LiteralChunk { Text = "some text" }
                    }
                },
                new ChunkTree
                {
                    Children = new Chunk[]
                    {
                        new UsingChunk { Namespace = "AppNamespace.Model" },
                    }
                }
            };

            var utility = new ChunkInheritanceUtility(host, cache, defaultChunks);
            var chunkTree = new ChunkTree();

            // Act
            utility.MergeInheritedChunkTrees(chunkTree, inheritedChunkTrees, "dynamic");

            // Assert
            Assert.Collection(chunkTree.Children,
                chunk => Assert.Same(defaultChunks[1], chunk),
                chunk => Assert.Same(inheritedChunkTrees[0].Children[0], chunk),
                chunk => Assert.Same(defaultChunks[0], chunk));
        }
    }
}