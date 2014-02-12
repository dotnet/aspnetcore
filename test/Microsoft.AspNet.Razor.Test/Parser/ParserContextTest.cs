// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Test.Framework;
using Microsoft.AspNet.Razor.Text;
using Microsoft.AspNet.Razor.Tokenizer.Symbols;
using Microsoft.TestCommon;
using Moq;

namespace Microsoft.AspNet.Razor.Test.Parser
{
    public class ParserContextTest
    {
        [Fact]
        public void ConstructorRequiresNonNullSource()
        {
            var codeParser = new CSharpCodeParser();
            Assert.ThrowsArgumentNull(() => new ParserContext(null, codeParser, new HtmlMarkupParser(), codeParser), "source");
        }

        [Fact]
        public void ConstructorRequiresNonNullCodeParser()
        {
            var codeParser = new CSharpCodeParser();
            Assert.ThrowsArgumentNull(() => new ParserContext(new SeekableTextReader(TextReader.Null), null, new HtmlMarkupParser(), codeParser), "codeParser");
        }

        [Fact]
        public void ConstructorRequiresNonNullMarkupParser()
        {
            var codeParser = new CSharpCodeParser();
            Assert.ThrowsArgumentNull(() => new ParserContext(new SeekableTextReader(TextReader.Null), codeParser, null, codeParser), "markupParser");
        }

        [Fact]
        public void ConstructorRequiresNonNullActiveParser()
        {
            Assert.ThrowsArgumentNull(() => new ParserContext(new SeekableTextReader(TextReader.Null), new CSharpCodeParser(), new HtmlMarkupParser(), null), "activeParser");
        }

        [Fact]
        public void ConstructorThrowsIfActiveParserIsNotCodeOrMarkupParser()
        {
            Assert.ThrowsArgument(() => new ParserContext(new SeekableTextReader(TextReader.Null), new CSharpCodeParser(), new HtmlMarkupParser(), new CSharpCodeParser()),
                                                    "activeParser",
                                                    RazorResources.ActiveParser_Must_Be_Code_Or_Markup_Parser);
        }

        [Fact]
        public void ConstructorAcceptsActiveParserIfIsSameAsEitherCodeOrMarkupParser()
        {
            var codeParser = new CSharpCodeParser();
            var markupParser = new HtmlMarkupParser();
            new ParserContext(new SeekableTextReader(TextReader.Null), codeParser, markupParser, codeParser);
            new ParserContext(new SeekableTextReader(TextReader.Null), codeParser, markupParser, markupParser);
        }

        [Fact]
        public void ConstructorInitializesProperties()
        {
            // Arrange
            SeekableTextReader expectedBuffer = new SeekableTextReader(TextReader.Null);
            CSharpCodeParser expectedCodeParser = new CSharpCodeParser();
            HtmlMarkupParser expectedMarkupParser = new HtmlMarkupParser();

            // Act
            ParserContext context = new ParserContext(expectedBuffer, expectedCodeParser, expectedMarkupParser, expectedCodeParser);

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
            ParserContext context = SetupTestContext("bar", b => b.Read());

            // Act
            char actual = context.CurrentCharacter;

            // Assert
            Assert.Equal('a', actual);
        }

        [Fact]
        public void CurrentCharacterReturnsNulCharacterIfTextBufferAtEOF()
        {
            // Arrange
            ParserContext context = SetupTestContext("bar", b => b.ReadToEnd());

            // Act
            char actual = context.CurrentCharacter;

            // Assert
            Assert.Equal('\0', actual);
        }

        [Fact]
        public void EndOfFileReturnsFalseIfTextBufferNotAtEOF()
        {
            // Arrange
            ParserContext context = SetupTestContext("bar");

            // Act/Assert
            Assert.False(context.EndOfFile);
        }

        [Fact]
        public void EndOfFileReturnsTrueIfTextBufferAtEOF()
        {
            // Arrange
            ParserContext context = SetupTestContext("bar", b => b.ReadToEnd());

            // Act/Assert
            Assert.True(context.EndOfFile);
        }

        [Fact]
        public void StartBlockCreatesNewBlock()
        {
            // Arrange
            ParserContext context = SetupTestContext("phoo");

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
            Mock<ParserVisitor> mockListener = new Mock<ParserVisitor>();
            ParserContext context = SetupTestContext("phoo");

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
            Mock<ParserVisitor> mockListener = new Mock<ParserVisitor>();
            ParserContext context = SetupTestContext("phoo");

            SpanBuilder builder = new SpanBuilder()
            {
                Kind = SpanKind.Code
            };
            builder.Accept(new CSharpSymbol(1, 0, 1, "foo", CSharpSymbolType.Identifier));
            Span added = builder.Build();

            using (context.StartBlock(BlockType.Functions))
            {
                context.AddSpan(added);
            }

            BlockBuilder expected = new BlockBuilder()
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
            ParserContext context = SetupTestContext("barbazbiz", b => b.Read(), codeParser, markupParser, codeParser);
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
            ParserContext context = SetupTestContext("barbazbiz", b => b.Read(), codeParser, markupParser, markupParser);
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

        private ParserContext SetupTestContext(string document, Action<TextReader> positioningAction, ParserBase codeParser, ParserBase markupParser, ParserBase activeParser)
        {
            ParserContext context = new ParserContext(new SeekableTextReader(new StringReader(document)), codeParser, markupParser, activeParser);
            positioningAction(context.Source);
            return context;
        }
    }
}
