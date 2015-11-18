// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Test.Framework;
using Xunit;

namespace Microsoft.AspNet.Razor.Chunks.Generators
{
    public class ChunkTreeBuilderTest
    {
        [Fact]
        public void AddAddTagHelperChunk_AddsChunkToTopLevelChunkTree()
        {
            // Arrange
            var spanFactory = SpanFactory.CreateCsHtml();
            var builder = new ChunkTreeBuilder();
            var block = new ExpressionBlock();
            var addTagHelperDirective = spanFactory.MetaCode(SyntaxConstants.CSharp.AddTagHelperKeyword + " ");

            // Act
            builder.StartParentChunk<ExpressionBlockChunk>(block);
            builder.AddAddTagHelperChunk("some text", addTagHelperDirective);
            builder.EndParentChunk();

            // Assert
            Assert.Equal(2, builder.Root.Children.Count);

            var parentChunk = Assert.IsType<ExpressionBlockChunk>(builder.Root.Children.First());
            Assert.Empty(parentChunk.Children);

            var addTagHelperChunk = Assert.IsType<AddTagHelperChunk>(builder.Root.Children.Last());
            Assert.Equal(addTagHelperChunk.LookupText, "some text");
        }

        [Fact]
        public void AddRemoveTagHelperChunk_AddsChunkToTopLevelChunkTree()
        {
            // Arrange
            var spanFactory = SpanFactory.CreateCsHtml();
            var builder = new ChunkTreeBuilder();
            var block = new ExpressionBlock();
            var removeTagHelperDirective = spanFactory.MetaCode(SyntaxConstants.CSharp.RemoveTagHelperKeyword + " ");

            // Act
            builder.StartParentChunk<ExpressionBlockChunk>(block);
            builder.AddRemoveTagHelperChunk("some text", removeTagHelperDirective);
            builder.EndParentChunk();

            // Assert
            Assert.Equal(2, builder.Root.Children.Count);

            var parentChunk = Assert.IsType<ExpressionBlockChunk>(builder.Root.Children.First());
            Assert.Empty(parentChunk.Children);

            var removeTagHelperChunk = Assert.IsType<RemoveTagHelperChunk>(builder.Root.Children.Last());
            Assert.Equal(removeTagHelperChunk.LookupText, "some text");
        }

        [Fact]
        public void AddLiteralChunk_AddsChunkToChunkTree()
        {
            // Arrange
            var spanFactory = SpanFactory.CreateCsHtml();
            var previousSpan = spanFactory.EmptyHtml().Builder.Build();
            var builder = new ChunkTreeBuilder();

            // Act
            builder.AddLiteralChunk("some text", previousSpan);

            // Assert
            var chunk = Assert.Single(builder.Root.Children);
            var literalChunk = Assert.IsType<LiteralChunk>(chunk);
            Assert.Equal("some text", literalChunk.Text);
            Assert.Same(previousSpan, literalChunk.Association);
        }

        [Fact]
        public void AddLiteralChunk_AppendsToPreviousChunk_IfChunkWasLiteral()
        {
            // Arrange
            var spanFactory = SpanFactory.CreateCsHtml();
            var previousSpan = spanFactory.Markup("<a>").Builder.Build();
            var newSpan = spanFactory.Markup("<p>").Builder.Build();
            var builder = new ChunkTreeBuilder();

            // Act
            builder.AddLiteralChunk("<a>", previousSpan);
            builder.AddLiteralChunk("<p>", newSpan);

            // Assert
            var chunk = Assert.Single(builder.Root.Children);
            var literalChunk = Assert.IsType<LiteralChunk>(chunk);
            Assert.Equal("<a><p>", literalChunk.Text);
            var span = Assert.IsType<Span>(literalChunk.Association);
            Assert.Equal(previousSpan.Symbols.Concat(newSpan.Symbols), span.Symbols);
        }

        [Fact]
        public void AddLiteralChunk_AddsChunkToChunkTree_IfPreviousChunkWasNotLiteral()
        {
            // Arrange
            var spanFactory = SpanFactory.CreateCsHtml();
            var codeSpan = spanFactory.Code("int a = 10;")
                                      .AsStatement()
                                      .Builder.Build();
            var literalSpan = spanFactory.Markup("<p>").Builder.Build();
            var builder = new ChunkTreeBuilder();

            // Act
            builder.AddStatementChunk("int a = 10;", codeSpan);
            builder.AddLiteralChunk("<p>", literalSpan);

            // Assert
            var chunks = builder.Root.Children;
            Assert.Equal(2, chunks.Count);
            var statementChunk = Assert.IsType<StatementChunk>(chunks[0]);
            Assert.Equal("int a = 10;", statementChunk.Code);
            Assert.Same(codeSpan, statementChunk.Association);
            var literalChunk = Assert.IsType<LiteralChunk>(chunks[1]);
            Assert.Equal("<p>", literalChunk.Text);
            Assert.Same(literalSpan, literalChunk.Association);
        }
    }
}