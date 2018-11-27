// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
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
        public void LookaheadUntil_PassesThePreviousTokensInTheSameOrder()
        {
            // Arrange
            var tokenizer = CreateContentTokenizer("asdf--fvd--<");

            // Act
            var i = 3;
            IEnumerable<HtmlToken> previousTokens = null;
            var tokenFound = tokenizer.LookaheadUntil((s, p) =>
            {
                previousTokens = p;
                return --i == 0;
            });

            // Assert
            Assert.Equal(4, previousTokens.Count());

            // For the very first element, there will be no previous items, so null is expected
            var orderIndex = 0;
            Assert.Null(previousTokens.ElementAt(orderIndex++));
            Assert.Equal(new HtmlToken("asdf", HtmlTokenType.Text), previousTokens.ElementAt(orderIndex++));
            Assert.Equal(new HtmlToken("--", HtmlTokenType.DoubleHyphen), previousTokens.ElementAt(orderIndex++));
            Assert.Equal(new HtmlToken("fvd", HtmlTokenType.Text), previousTokens.ElementAt(orderIndex++));
        }

        [Fact]
        public void LookaheadUntil_ReturnsFalseAfterIteratingOverAllTokensIfConditionIsNotMet()
        {
            // Arrange
            var tokenizer = CreateContentTokenizer("asdf--fvd");

            // Act
            var tokens = new Stack<HtmlToken>();
            var tokenFound = tokenizer.LookaheadUntil((s, p) =>
            {
                tokens.Push(s);
                return false;
            });

            // Assert
            Assert.False(tokenFound);
            Assert.Equal(3, tokens.Count);
            Assert.Equal(new HtmlToken("fvd", HtmlTokenType.Text), tokens.Pop());
            Assert.Equal(new HtmlToken("--", HtmlTokenType.DoubleHyphen), tokens.Pop());
            Assert.Equal(new HtmlToken("asdf", HtmlTokenType.Text), tokens.Pop());
        }

        [Fact]
        public void LookaheadUntil_ReturnsTrueAndBreaksIteration()
        {
            // Arrange
            var tokenizer = CreateContentTokenizer("asdf--fvd");

            // Act
            var tokens = new Stack<HtmlToken>();
            var tokenFound = tokenizer.LookaheadUntil((s, p) =>
            {
                tokens.Push(s);
                return s.Type == HtmlTokenType.DoubleHyphen;
            });

            // Assert
            Assert.True(tokenFound);
            Assert.Equal(2, tokens.Count);
            Assert.Equal(new HtmlToken("--", HtmlTokenType.DoubleHyphen), tokens.Pop());
            Assert.Equal(new HtmlToken("asdf", HtmlTokenType.Text), tokens.Pop());
        }

        private static TestTokenizerBackedParser CreateContentTokenizer(string content)
        {
            var source = TestRazorSourceDocument.Create(content);
            var options = RazorParserOptions.CreateDefault();
            var context = new ParserContext(source, options);

            var tokenizer = new TestTokenizerBackedParser(HtmlLanguageCharacteristics.Instance, context);
            return tokenizer;
        }

        private class ExposedTokenizer : Tokenizer<CSharpToken, CSharpTokenType>
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

            public override CSharpTokenType RazorCommentStarType
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override CSharpTokenType RazorCommentTransitionType
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override CSharpTokenType RazorCommentType
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

            protected override CSharpToken CreateToken(
                string content,
                CSharpTokenType type,
                IReadOnlyList<RazorDiagnostic> errors)
            {
                throw new NotImplementedException();
            }

            protected override StateResult Dispatch()
            {
                throw new NotImplementedException();
            }
        }

        private class TestTokenizerBackedParser : TokenizerBackedParser<HtmlTokenizer, HtmlToken, HtmlTokenType>
        {
            internal TestTokenizerBackedParser(LanguageCharacteristics<HtmlTokenizer, HtmlToken, HtmlTokenType> language, ParserContext context) : base(language, context)
            {
            }

            public override void ParseBlock()
            {
                throw new NotImplementedException();
            }

            protected override bool TokenTypeEquals(HtmlTokenType x, HtmlTokenType y)
            {
                throw new NotImplementedException();
            }

            internal new bool LookaheadUntil(Func<HtmlToken, IEnumerable<HtmlToken>, bool> condition)
            {
                return base.LookaheadUntil(condition);
            }
        }
    }
}
