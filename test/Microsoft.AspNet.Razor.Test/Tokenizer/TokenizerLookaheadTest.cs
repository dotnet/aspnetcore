// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNet.Razor.Text;
using Microsoft.AspNet.Razor.Tokenizer;
using Microsoft.AspNet.Razor.Tokenizer.Symbols;
using Xunit;

namespace Microsoft.AspNet.Razor.Test.Tokenizer
{
    public class TokenizerLookaheadTest : HtmlTokenizerTestBase
    {
        [Fact]
        public void After_Cancelling_Lookahead_Tokenizer_Returns_Same_Tokens_As_It_Did_Before_Lookahead()
        {
            var tokenizer = new HtmlTokenizer(new SeekableTextReader(new StringReader("<foo>")));
            using (tokenizer.Source.BeginLookahead())
            {
                Assert.Equal(new HtmlSymbol(0, 0, 0, "<", HtmlSymbolType.OpenAngle), tokenizer.NextSymbol());
                Assert.Equal(new HtmlSymbol(1, 0, 1, "foo", HtmlSymbolType.Text), tokenizer.NextSymbol());
                Assert.Equal(new HtmlSymbol(4, 0, 4, ">", HtmlSymbolType.CloseAngle), tokenizer.NextSymbol());
            }
            Assert.Equal(new HtmlSymbol(0, 0, 0, "<", HtmlSymbolType.OpenAngle), tokenizer.NextSymbol());
            Assert.Equal(new HtmlSymbol(1, 0, 1, "foo", HtmlSymbolType.Text), tokenizer.NextSymbol());
            Assert.Equal(new HtmlSymbol(4, 0, 4, ">", HtmlSymbolType.CloseAngle), tokenizer.NextSymbol());
        }

        [Fact]
        public void After_Accepting_Lookahead_Tokenizer_Returns_Next_Token()
        {
            var tokenizer = new HtmlTokenizer(new SeekableTextReader(new StringReader("<foo>")));
            using (LookaheadToken lookahead = tokenizer.Source.BeginLookahead())
            {
                Assert.Equal(new HtmlSymbol(0, 0, 0, "<", HtmlSymbolType.OpenAngle), tokenizer.NextSymbol());
                Assert.Equal(new HtmlSymbol(1, 0, 1, "foo", HtmlSymbolType.Text), tokenizer.NextSymbol());
                lookahead.Accept();
            }
            Assert.Equal(new HtmlSymbol(4, 0, 4, ">", HtmlSymbolType.CloseAngle), tokenizer.NextSymbol());
        }
    }
}
