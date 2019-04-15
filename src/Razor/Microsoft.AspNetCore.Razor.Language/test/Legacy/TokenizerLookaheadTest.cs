// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;
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
            IEnumerable<SyntaxToken> previousTokens = null;
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
            AssertTokenEqual(SyntaxFactory.Token(SyntaxKind.Text, "asdf"), previousTokens.ElementAt(orderIndex++));
            AssertTokenEqual(SyntaxFactory.Token(SyntaxKind.DoubleHyphen, "--"), previousTokens.ElementAt(orderIndex++));
            AssertTokenEqual(SyntaxFactory.Token(SyntaxKind.Text, "fvd"), previousTokens.ElementAt(orderIndex++));
        }

        [Fact]
        public void LookaheadUntil_ReturnsFalseAfterIteratingOverAllTokensIfConditionIsNotMet()
        {
            // Arrange
            var tokenizer = CreateContentTokenizer("asdf--fvd");

            // Act
            var tokens = new Stack<SyntaxToken>();
            var tokenFound = tokenizer.LookaheadUntil((s, p) =>
            {
                tokens.Push(s);
                return false;
            });

            // Assert
            Assert.False(tokenFound);
            Assert.Equal(3, tokens.Count);
            AssertTokenEqual(SyntaxFactory.Token(SyntaxKind.Text, "fvd"), tokens.Pop());
            AssertTokenEqual(SyntaxFactory.Token(SyntaxKind.DoubleHyphen, "--"), tokens.Pop());
            AssertTokenEqual(SyntaxFactory.Token(SyntaxKind.Text, "asdf"), tokens.Pop());
        }

        [Fact]
        public void LookaheadUntil_ReturnsTrueAndBreaksIteration()
        {
            // Arrange
            var tokenizer = CreateContentTokenizer("asdf--fvd");

            // Act
            var tokens = new Stack<SyntaxToken>();
            var tokenFound = tokenizer.LookaheadUntil((s, p) =>
            {
                tokens.Push(s);
                return s.Kind == SyntaxKind.DoubleHyphen;
            });

            // Assert
            Assert.True(tokenFound);
            Assert.Equal(2, tokens.Count);
            AssertTokenEqual(SyntaxFactory.Token(SyntaxKind.DoubleHyphen, "--"), tokens.Pop());
            AssertTokenEqual(SyntaxFactory.Token(SyntaxKind.Text, "asdf"), tokens.Pop());
        }

        private static TestTokenizerBackedParser CreateContentTokenizer(string content)
        {
            var source = TestRazorSourceDocument.Create(content);
            var options = RazorParserOptions.CreateDefault();
            var context = new ParserContext(source, options);

            var tokenizer = new TestTokenizerBackedParser(HtmlLanguageCharacteristics.Instance, context);
            return tokenizer;
        }

        private static void AssertTokenEqual(SyntaxToken expected, SyntaxToken actual)
        {
            Assert.True(expected.IsEquivalentTo(actual), "Tokens not equal.");
        }

        private class ExposedTokenizer : Tokenizer
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

            public override SyntaxKind RazorCommentStarKind
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override SyntaxKind RazorCommentTransitionKind
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override SyntaxKind RazorCommentKind
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

            protected override SyntaxToken CreateToken(
                string content,
                SyntaxKind type,
                IReadOnlyList<RazorDiagnostic> errors)
            {
                throw new NotImplementedException();
            }

            protected override StateResult Dispatch()
            {
                throw new NotImplementedException();
            }
        }

        private class TestTokenizerBackedParser : TokenizerBackedParser<HtmlTokenizer>
        {
            internal TestTokenizerBackedParser(LanguageCharacteristics<HtmlTokenizer> language, ParserContext context) : base(language, context)
            {
            }

            internal new bool LookaheadUntil(Func<SyntaxToken, IEnumerable<SyntaxToken>, bool> condition)
            {
                return base.LookaheadUntil(condition);
            }
        }
    }
}
