// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public abstract class CSharpTokenizerTestBase : TokenizerTestBase
    {
        private static CSharpToken _ignoreRemaining = new CSharpToken(string.Empty, CSharpTokenType.Unknown);

        internal override object IgnoreRemaining
        {
            get { return _ignoreRemaining; }
        }

        internal override object CreateTokenizer(ITextDocument source)
        {
            return new CSharpTokenizer(source);
        }

        internal void TestSingleToken(string text, CSharpTokenType expectedTokenType)
        {
            TestTokenizer(text, new CSharpToken(text, expectedTokenType));
        }

        internal void TestTokenizer(string input, params CSharpToken[] expectedTokens)
        {
            base.TestTokenizer<CSharpToken, CSharpTokenType>(input, expectedTokens);
        }
    }
}
