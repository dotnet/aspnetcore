// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public abstract class HtmlTokenizerTestBase : TokenizerTestBase
    {
        private static HtmlToken _ignoreRemaining = new HtmlToken(string.Empty, HtmlTokenType.Unknown);

        internal override object IgnoreRemaining
        {
            get { return _ignoreRemaining; }
        }

        internal override object CreateTokenizer(ITextDocument source)
        {
            return new HtmlTokenizer(source);
        }

        internal void TestSingleToken(string text, HtmlTokenType expectedTokenType)
        {
            TestTokenizer(text, new HtmlToken(text, expectedTokenType));
        }

        internal void TestTokenizer(string input, params HtmlToken[] expectedTokens)
        {
            base.TestTokenizer<HtmlToken, HtmlTokenType>(input, expectedTokens);
        }
    }
}
