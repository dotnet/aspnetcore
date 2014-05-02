// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System.IO;
using Microsoft.AspNet.Razor.Text;
using Microsoft.AspNet.Razor.Tokenizer;
using Microsoft.AspNet.Razor.Tokenizer.Symbols;
using Microsoft.TestCommon;

namespace Microsoft.AspNet.Razor.Test.Tokenizer
{
    public class TokenizerLookaheadTest : HtmlTokenizerTestBase
    {
        [Fact]
        public void After_Cancelling_Lookahead_Tokenizer_Returns_Same_Tokens_As_It_Did_Before_Lookahead()
        {
            HtmlTokenizer tokenizer = new HtmlTokenizer(new SeekableTextReader(new StringReader("<foo>")));
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
            HtmlTokenizer tokenizer = new HtmlTokenizer(new SeekableTextReader(new StringReader("<foo>")));
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
