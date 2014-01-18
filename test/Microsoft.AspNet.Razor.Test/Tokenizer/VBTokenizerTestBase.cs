// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.Text;
using Microsoft.AspNet.Razor.Tokenizer;
using Microsoft.AspNet.Razor.Tokenizer.Symbols;

namespace Microsoft.AspNet.Razor.Test.Tokenizer
{
    public abstract class VBTokenizerTestBase : TokenizerTestBase<VBSymbol, VBSymbolType>
    {
        private static VBSymbol _ignoreRemaining = new VBSymbol(0, 0, 0, String.Empty, VBSymbolType.Unknown);

        protected override VBSymbol IgnoreRemaining
        {
            get { return _ignoreRemaining; }
        }

        protected override Tokenizer<VBSymbol, VBSymbolType> CreateTokenizer(ITextDocument source)
        {
            return new VBTokenizer(source);
        }

        protected void TestSingleToken(string text, VBSymbolType expectedSymbolType)
        {
            TestTokenizer(text, new VBSymbol(0, 0, 0, text, expectedSymbolType));
        }
    }
}
