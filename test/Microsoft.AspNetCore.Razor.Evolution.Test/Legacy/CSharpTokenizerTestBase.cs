// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    public abstract class CSharpTokenizerTestBase : TokenizerTestBase
    {
        private static CSharpSymbol _ignoreRemaining = new CSharpSymbol(0, 0, 0, string.Empty, CSharpSymbolType.Unknown);

        internal override object IgnoreRemaining
        {
            get { return _ignoreRemaining; }
        }

        internal override object CreateTokenizer(ITextDocument source)
        {
            return new CSharpTokenizer(source);
        }

        internal void TestSingleToken(string text, CSharpSymbolType expectedSymbolType)
        {
            TestTokenizer(text, new CSharpSymbol(0, 0, 0, text, expectedSymbolType));
        }

        internal void TestTokenizer(string input, params CSharpSymbol[] expectedSymbols)
        {
            base.TestTokenizer<CSharpSymbol, CSharpSymbolType>(input, expectedSymbols);
        }
    }
}
