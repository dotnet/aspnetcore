// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class TokenizerLookaheadTest : HtmlTokenizerTestBase
    {
        [Fact]
        public void Lookahead_MaintainsExistingBufferWhenRejected()
        {
            // Arrange
            var tokenizer = new ExposedTokenizer("01234");
            tokenizer.Buffer.Append("pre-existing values");

            // Act
            var result = tokenizer.Lookahead("0x", takeIfMatch: true, caseSensitive: true);

            // Assert
            Assert.False(result);
            Assert.Equal("pre-existing values", tokenizer.Buffer.ToString(), StringComparer.Ordinal);
        }

        [Fact]
        public void Lookahead_AddsToExistingBufferWhenSuccessfulAndTakeIfMatchIsTrue()
        {
            // Arrange
            var tokenizer = new ExposedTokenizer("0x1234");
            tokenizer.Buffer.Append("pre-existing values");

            // Act
            var result = tokenizer.Lookahead("0x", takeIfMatch: true, caseSensitive: true);

            // Assert
            Assert.True(result);
            Assert.Equal("pre-existing values0x", tokenizer.Buffer.ToString(), StringComparer.Ordinal);
        }

        [Fact]
        public void Lookahead_MaintainsExistingBufferWhenSuccessfulAndTakeIfMatchIsFalse()
        {
            // Arrange
            var tokenizer = new ExposedTokenizer("0x1234");
            tokenizer.Buffer.Append("pre-existing values");

            // Act
            var result = tokenizer.Lookahead("0x", takeIfMatch: false, caseSensitive: true);

            // Assert
            Assert.True(result);
            Assert.Equal("pre-existing values", tokenizer.Buffer.ToString(), StringComparer.Ordinal);
        }

        [Fact]
        public void LookaheadUntil_PassesThePreviousSymbolsInReverseOrder()
        {
            // Arrange
            var source = TestRazorSourceDocument.Create("asdf--fvd--<");
            var options = RazorParserOptions.CreateDefault();
            var context = new ParserContext(source, options);

            var tokenizer = new TestTokenizerBackedParser(HtmlLanguageCharacteristics.Instance, context);

            // Act
            Stack<HtmlSymbol> symbols = new Stack<HtmlSymbol>();
            var i = 3;
            var symbolFound = tokenizer.LookaheadUntil((s, p) =>
            {
                symbols.Push(s);
                return --i == 0;
            });

            // Assert
            Assert.Equal(3, symbols.Count);
            Assert.Equal(new HtmlSymbol("fvd", HtmlSymbolType.Text), symbols.Pop());
            Assert.Equal(new HtmlSymbol("--", HtmlSymbolType.DoubleHyphen), symbols.Pop());
            Assert.Equal(new HtmlSymbol("asdf", HtmlSymbolType.Text), symbols.Pop());
        }

        [Fact]
        public void LookaheadUntil_ReturnsFalseAfterIteratingOverAllSymbolsIfConditionIsNotMet()
        {
            // Arrange
            var source = TestRazorSourceDocument.Create("asdf--fvd");
            var options = RazorParserOptions.CreateDefault();
            var context = new ParserContext(source, options);

            var tokenizer = new TestTokenizerBackedParser(HtmlLanguageCharacteristics.Instance, context);

            // Act
            Stack<HtmlSymbol> symbols = new Stack<HtmlSymbol>();
            var symbolFound = tokenizer.LookaheadUntil((s, p) =>
            {
                symbols.Push(s);
                return false;
            });

            // Assert
            Assert.False(symbolFound);
            Assert.Equal(3, symbols.Count);
            Assert.Equal(new HtmlSymbol("fvd", HtmlSymbolType.Text), symbols.Pop());
            Assert.Equal(new HtmlSymbol("--", HtmlSymbolType.DoubleHyphen), symbols.Pop());
            Assert.Equal(new HtmlSymbol("asdf", HtmlSymbolType.Text), symbols.Pop());
        }

        [Fact]
        public void LookaheadUntil_ReturnsTrueAndBreaksIteration()
        {
            // Arrange
            var source = TestRazorSourceDocument.Create("asdf--fvd");
            var options = RazorParserOptions.CreateDefault();
            var context = new ParserContext(source, options);

            var tokenizer = new TestTokenizerBackedParser(HtmlLanguageCharacteristics.Instance, context);

            // Act
            Stack<HtmlSymbol> symbols = new Stack<HtmlSymbol>();
            var symbolFound = tokenizer.LookaheadUntil((s, p) =>
            {
                symbols.Push(s);
                return s.Type == HtmlSymbolType.DoubleHyphen;
            });

            // Assert
            Assert.True(symbolFound);
            Assert.Equal(2, symbols.Count);
            Assert.Equal(new HtmlSymbol("--", HtmlSymbolType.DoubleHyphen), symbols.Pop());
            Assert.Equal(new HtmlSymbol("asdf", HtmlSymbolType.Text), symbols.Pop());
        }

        private class ExposedTokenizer : Tokenizer<CSharpSymbol, CSharpSymbolType>
        {
            public ExposedTokenizer(string input)
                : base(new SeekableTextReader(input, filePath: null))
            {
            }

            public new StringBuilder Buffer
            {
                get
                {
                    return base.Buffer;
                }
            }

            public override CSharpSymbolType RazorCommentStarType
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override CSharpSymbolType RazorCommentTransitionType
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override CSharpSymbolType RazorCommentType
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            protected override int StartState
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            protected override CSharpSymbol CreateSymbol(
                string content,
                CSharpSymbolType type,
                IReadOnlyList<RazorDiagnostic> errors)
            {
                throw new NotImplementedException();
            }

            protected override StateResult Dispatch()
            {
                throw new NotImplementedException();
            }
        }

        private class TestTokenizerBackedParser : TokenizerBackedParser<HtmlTokenizer, HtmlSymbol, HtmlSymbolType>
        {
            internal TestTokenizerBackedParser(LanguageCharacteristics<HtmlTokenizer, HtmlSymbol, HtmlSymbolType> language, ParserContext context) : base(language, context)
            {
            }

            public override void ParseBlock()
            {
                throw new NotImplementedException();
            }

            protected override bool SymbolTypeEquals(HtmlSymbolType x, HtmlSymbolType y)
            {
                throw new NotImplementedException();
            }

            internal new bool LookaheadUntil(Func<HtmlSymbol, IEnumerable<HtmlSymbol>, bool> condition)
            {
                return base.LookaheadUntil(condition);
            }
        }
    }
}
