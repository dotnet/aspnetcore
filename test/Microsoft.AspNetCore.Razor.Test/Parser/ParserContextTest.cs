// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Razor.Parser;
using Microsoft.AspNetCore.Razor.Parser.Internal;
using Microsoft.AspNetCore.Razor.Parser.SyntaxTree;
using Microsoft.AspNetCore.Razor.Test.Framework;
using Microsoft.AspNetCore.Razor.Text;
using Microsoft.AspNetCore.Razor.Tokenizer.Symbols;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Test.Parser
{
    public class ParserContextTest
    {
        [Fact]
        public void ConstructorThrowsIfActiveParserIsNotCodeOrMarkupParser()
        {
            var parameterName = "activeParser";
            var exception = Assert.Throws<ArgumentException>(parameterName,
                                                             () => new ParserContext(
                                                                 source: new SeekableTextReader(TextReader.Null),
                                                                 codeParser: new CSharpCodeParser(),
                                                                 markupParser: new HtmlMarkupParser(),
                                                                 activeParser: new CSharpCodeParser(),
                                                                 errorSink: new ErrorSink()));
            ExceptionHelpers.ValidateArgumentException(parameterName, RazorResources.ActiveParser_Must_Be_Code_Or_Markup_Parser, exception);
        }

        [Fact]
        public void ConstructorAcceptsActiveParserIfIsSameAsEitherCodeOrMarkupParser()
        {
            var codeParser = new CSharpCodeParser();
            var markupParser = new HtmlMarkupParser();
            var errorSink = new ErrorSink();
            new ParserContext(
                new SeekableTextReader(TextReader.Null), codeParser, markupParser, codeParser, errorSink);
            new ParserContext(
                new SeekableTextReader(TextReader.Null), codeParser, markupParser, markupParser, errorSink);
        }

        [Fact]
        public void ConstructorInitializesProperties()
        {
            // Arrange
            var expectedBuffer = new SeekableTextReader(TextReader.Null);
            var expectedCodeParser = new CSharpCodeParser();
            var expectedMarkupParser = new HtmlMarkupParser();

            // Act
            var context = new ParserContext(expectedBuffer,
                                            expectedCodeParser,
                                            expectedMarkupParser,
                                            expectedCodeParser,
                                            new ErrorSink());

            // Assert
            Assert.NotNull(context.Source);
            Assert.Same(expectedCodeParser, context.CodeParser);
            Assert.Same(expectedMarkupParser, context.MarkupParser);
            Assert.Same(expectedCodeParser, context.ActiveParser);
        }

        [Fact]
        public void CurrentCharacterReturnsCurrentCharacterInTextBuffer()
        {
            // Arrange
            var context = SetupTestContext("bar", b => b.Read());

            // Act
            var actual = context.CurrentCharacter;

            // Assert
            Assert.Equal('a', actual);
        }

        [Fact]
        public void CurrentCharacterReturnsNulCharacterIfTextBufferAtEOF()
        {
            // Arrange
            var context = SetupTestContext("bar", b => b.ReadToEnd());

            // Act
            var actual = context.CurrentCharacter;

            // Assert
            Assert.Equal('\0', actual);
        }

        [Fact]
        public void EndOfFileReturnsFalseIfTextBufferNotAtEOF()
        {
            // Arrange
            var context = SetupTestContext("bar");

            // Act/Assert
            Assert.False(context.EndOfFile);
        }

        [Fact]
        public void EndOfFileReturnsTrueIfTextBufferAtEOF()
        {
            // Arrange
            var context = SetupTestContext("bar", b => b.ReadToEnd());

            // Act/Assert
            Assert.True(context.EndOfFile);
        }

        [Fact]
        public void StartBlockCreatesNewBlock()
        {
            // Arrange
            var context = SetupTestContext("phoo");

            // Act
            context.StartBlock(BlockType.Expression);

            // Assert
            Assert.Equal(1, context.BlockStack.Count);
            Assert.Equal(BlockType.Expression, context.BlockStack.Peek().Type);
        }

        [Fact]
        public void EndBlockAddsCurrentBlockToParentBlock()
        {
            // Arrange
            var mockListener = new Mock<ParserVisitor>();
            var context = SetupTestContext("phoo");

            // Act
            context.StartBlock(BlockType.Expression);
            context.StartBlock(BlockType.Statement);
            context.EndBlock();

            // Assert
            Assert.Equal(1, context.BlockStack.Count);
            Assert.Equal(BlockType.Expression, context.BlockStack.Peek().Type);
            Assert.Equal(1, context.BlockStack.Peek().Children.Count);
            Assert.Equal(BlockType.Statement, ((Block)context.BlockStack.Peek().Children[0]).Type);
        }

        [Fact]
        public void AddSpanAddsSpanToCurrentBlockBuilder()
        {
            // Arrange
            var factory = SpanFactory.CreateCsHtml();
            var mockListener = new Mock<ParserVisitor>();
            var context = SetupTestContext("phoo");

            var builder = new SpanBuilder()
            {
                Kind = SpanKind.Code
            };
            builder.Accept(new CSharpSymbol(1, 0, 1, "foo", CSharpSymbolType.Identifier));
            var added = builder.Build();

            using (context.StartBlock(BlockType.Functions))
            {
                context.AddSpan(added);
            }

            var expected = new BlockBuilder()
            {
                Type = BlockType.Functions,
            };
            expected.Children.Add(added);

            // Assert
            ParserTestBase.EvaluateResults(context.CompleteParse(), expected.Build());
        }

        [Fact]
        public void SwitchActiveParserSetsMarkupParserAsActiveIfCodeParserCurrentlyActive()
        {
            // Arrange
            var codeParser = new CSharpCodeParser();
            var markupParser = new HtmlMarkupParser();
            var context = SetupTestContext("barbazbiz", b => b.Read(), codeParser, markupParser, codeParser);
            Assert.Same(codeParser, context.ActiveParser);

            // Act
            context.SwitchActiveParser();

            // Assert
            Assert.Same(markupParser, context.ActiveParser);
        }

        [Fact]
        public void SwitchActiveParserSetsCodeParserAsActiveIfMarkupParserCurrentlyActive()
        {
            // Arrange
            var codeParser = new CSharpCodeParser();
            var markupParser = new HtmlMarkupParser();
            var context = SetupTestContext("barbazbiz", b => b.Read(), codeParser, markupParser, markupParser);
            Assert.Same(markupParser, context.ActiveParser);

            // Act
            context.SwitchActiveParser();

            // Assert
            Assert.Same(codeParser, context.ActiveParser);
        }

        private ParserContext SetupTestContext(string document)
        {
            var codeParser = new CSharpCodeParser();
            var markupParser = new HtmlMarkupParser();
            return SetupTestContext(document, b => { }, codeParser, markupParser, codeParser);
        }

        private ParserContext SetupTestContext(string document, Action<TextReader> positioningAction)
        {
            var codeParser = new CSharpCodeParser();
            var markupParser = new HtmlMarkupParser();
            return SetupTestContext(document, positioningAction, codeParser, markupParser, codeParser);
        }

        private ParserContext SetupTestContext(string document,
                                               Action<TextReader> positioningAction,
                                               ParserBase codeParser,
                                               ParserBase markupParser,
                                               ParserBase activeParser)
        {
            var context = new ParserContext(
                new SeekableTextReader(new StringReader(document)),
                codeParser,
                markupParser,
                activeParser,
                new ErrorSink());

            positioningAction(context.Source);
            return context;
        }
    }
}
