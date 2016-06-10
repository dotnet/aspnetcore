// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Razor.Parser;
using Microsoft.AspNetCore.Razor.Parser.SyntaxTree;
using Microsoft.AspNetCore.Razor.Test.Framework;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Chunks
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

            var literalChunk = Assert.IsType<ParentLiteralChunk>(chunk);
            Assert.Equal(2, literalChunk.Children.Count);

            var span = Assert.IsType<Span>(literalChunk.Children[0].Association);
            Assert.Same(span, previousSpan);

            span = Assert.IsType<Span>(literalChunk.Children[1].Association);
            Assert.Same(span, newSpan);
        }

        [Fact]
        public void AddLiteralChunk_CreatesNewChunk_IfChunkIsNotLiteral()
        {
            // Arrange
            var spanFactory = SpanFactory.CreateCsHtml();
            var span1 = spanFactory.Markup("<a>").Builder.Build();
            var span2 = spanFactory.Markup("<p>").Builder.Build();
            var span3 = spanFactory.Code("Hi!").AsExpression().Builder.Build();
            var builder = new ChunkTreeBuilder();

            // Act
            builder.AddLiteralChunk("<a>", span1);
            builder.AddLiteralChunk("<p>", span2);
            builder.AddExpressionChunk("Hi!", span3);

            // Assert
            Assert.Equal(2, builder.Root.Children.Count);

            var literalChunk = Assert.IsType<ParentLiteralChunk>(builder.Root.Children[0]);
            Assert.Equal(2, literalChunk.Children.Count);

            var span = Assert.IsType<Span>(literalChunk.Children[0].Association);
            Assert.Same(span, span1);

            span = Assert.IsType<Span>(literalChunk.Children[1].Association);
            Assert.Same(span, span2);

            Assert.IsType<ExpressionChunk>(builder.Root.Children[1]);
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