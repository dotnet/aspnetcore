// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public abstract class CSharpTokenizerTestBase : TokenizerTestBase
    {
        private static SyntaxToken _ignoreRemaining = SyntaxFactory.Token(SyntaxKind.Marker, string.Empty);

        internal override object IgnoreRemaining
        {
            get { return _ignoreRemaining; }
        }

        internal override object CreateTokenizer(ITextDocument source)
        {
            return new CSharpTokenizer(source);
        }

        internal void TestSingleToken(string text, SyntaxKind expectedTokenKind)
        {
            TestTokenizer(text, SyntaxFactory.Token(expectedTokenKind, text));
        }
    }
}
