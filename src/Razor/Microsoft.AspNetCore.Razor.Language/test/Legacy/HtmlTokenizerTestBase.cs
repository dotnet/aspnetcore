// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

public abstract class HtmlTokenizerTestBase : TokenizerTestBase
{
    private static readonly SyntaxToken _ignoreRemaining = SyntaxFactory.Token(SyntaxKind.Marker, string.Empty);

    internal override object IgnoreRemaining
    {
        get { return _ignoreRemaining; }
    }

    internal override object CreateTokenizer(ITextDocument source)
    {
        return new HtmlTokenizer(source);
    }

    internal void TestSingleToken(string text, SyntaxKind expectedTokenKind)
    {
        TestTokenizer(text, SyntaxFactory.Token(expectedTokenKind, text));
    }
}
