// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Test.Framework;
using System.Linq;
using Xunit;
using Microsoft.AspNet.Razor.Parser;

namespace Microsoft.AspNet.Razor
{
    public class CodeTreeBuilderTest
    {
        [Fact]
        public void AddAddTagHelperChunk_AddsChunkToTopLevelCodeTree()
        {
            // Arrange
            var spanFactory = SpanFactory.CreateCsHtml();
            var builder = new CodeTreeBuilder();
            var block = new ExpressionBlock();
            var addTagHelperDirective = spanFactory.MetaCode(SyntaxConstants.CSharp.AddTagHelperKeyword + " ");

            // Act 
            builder.StartChunkBlock<ExpressionBlockChunk>(block);
            builder.AddAddTagHelperChunk("some text", addTagHelperDirective);
            builder.EndChunkBlock();

            // Assert
            Assert.Equal(2, builder.CodeTree.Chunks.Count);

            var chunkBlock = Assert.IsType<ExpressionBlockChunk>(builder.CodeTree.Chunks.First());
            Assert.Empty(chunkBlock.Children);

            var addTagHelperChunk = Assert.IsType<AddTagHelperChunk>(builder.CodeTree.Chunks.Last());
            Assert.Equal(addTagHelperChunk.LookupText, "some text");
        }

        [Fact]
        public void AddRemoveTagHelperChunk_AddsChunkToTopLevelCodeTree()
        {
            // Arrange
            var spanFactory = SpanFactory.CreateCsHtml();
            var builder = new CodeTreeBuilder();
            var block = new ExpressionBlock();
            var removeTagHelperDirective = spanFactory.MetaCode(SyntaxConstants.CSharp.RemoveTagHelperKeyword + " ");

            // Act 
            builder.StartChunkBlock<ExpressionBlockChunk>(block);
            builder.AddRemoveTagHelperChunk("some text", removeTagHelperDirective);
            builder.EndChunkBlock();

            // Assert
            Assert.Equal(2, builder.CodeTree.Chunks.Count);

            var chunkBlock = Assert.IsType<ExpressionBlockChunk>(builder.CodeTree.Chunks.First());
            Assert.Empty(chunkBlock.Children);

            var removeTagHelperChunk = Assert.IsType<RemoveTagHelperChunk>(builder.CodeTree.Chunks.Last());
            Assert.Equal(removeTagHelperChunk.LookupText, "some text");
        }

        [Fact]
        public void AddLiteralChunk_AddsChunkToCodeTree()
        {
            // Arrange
            var spanFactory = SpanFactory.CreateCsHtml();
            var previousSpan = spanFactory.EmptyHtml().Builder.Build();
            var builder = new CodeTreeBuilder();
            
            // Act 
            builder.AddLiteralChunk("some text", previousSpan);

            // Assert
            var chunk = Assert.Single(builder.CodeTree.Chunks);
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
            var builder = new CodeTreeBuilder();

            // Act 
            builder.AddLiteralChunk("<a>", previousSpan);
            builder.AddLiteralChunk("<p>", newSpan);

            // Assert
            var chunk = Assert.Single(builder.CodeTree.Chunks);
            var literalChunk = Assert.IsType<LiteralChunk>(chunk);
            Assert.Equal("<a><p>", literalChunk.Text);
            var span = Assert.IsType<Span>(literalChunk.Association);
            Assert.Equal(previousSpan.Symbols.Concat(newSpan.Symbols), span.Symbols);
        }

        [Fact]
        public void AddLiteralChunk_AddsChunkToCodeTree_IfPreviousChunkWasNotLiteral()
        {
            // Arrange
            var spanFactory = SpanFactory.CreateCsHtml();
            var codeSpan = spanFactory.Code("int a = 10;")
                                      .AsStatement()
                                      .Builder.Build();
            var literalSpan = spanFactory.Markup("<p>").Builder.Build();
            var builder = new CodeTreeBuilder();

            // Act 
            builder.AddStatementChunk("int a = 10;", codeSpan);
            builder.AddLiteralChunk("<p>", literalSpan);

            // Assert
            var chunks = builder.CodeTree.Chunks;
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