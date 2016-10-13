// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    internal abstract class HtmlTokenizerTestBase : TokenizerTestBase<HtmlSymbol, HtmlSymbolType>
    {
        private static HtmlSymbol _ignoreRemaining = new HtmlSymbol(0, 0, 0, string.Empty, HtmlSymbolType.Unknown);

        protected override HtmlSymbol IgnoreRemaining
        {
            get { return _ignoreRemaining; }
        }

        protected override Tokenizer<HtmlSymbol, HtmlSymbolType> CreateTokenizer(ITextDocument source)
        {
            return new HtmlTokenizer(source);
        }

        protected void TestSingleToken(string text, HtmlSymbolType expectedSymbolType)
        {
            TestTokenizer(text, new HtmlSymbol(0, 0, 0, text, expectedSymbolType));
        }
    }
}
