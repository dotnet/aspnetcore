// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    internal class TokenizerLookaheadTest : HtmlTokenizerTestBase
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

        private class ExposedTokenizer : Tokenizer<CSharpSymbol, CSharpSymbolType>
        {
            public ExposedTokenizer(string input)
                : base(new SeekableTextReader(new StringReader(input)))
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
                SourceLocation start,
                string content,
                CSharpSymbolType type,
                IReadOnlyList<RazorError> errors)
            {
                throw new NotImplementedException();
            }

            protected override StateResult Dispatch()
            {
                throw new NotImplementedException();
            }
        }
    }
}
