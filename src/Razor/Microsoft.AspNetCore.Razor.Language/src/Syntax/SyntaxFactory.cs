// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal static partial class SyntaxFactory
{
    public static SyntaxToken Token(SyntaxKind kind, params RazorDiagnostic[] diagnostics)
    {
        return Token(kind, content: string.Empty, diagnostics: diagnostics);
    }

    public static SyntaxToken Token(SyntaxKind kind, string content, params RazorDiagnostic[] diagnostics)
    {
        return new SyntaxToken(InternalSyntax.SyntaxFactory.Token(kind, content), parent: null, position: 0);
    }

    internal static SyntaxToken MissingToken(SyntaxKind kind, params RazorDiagnostic[] diagnostics)
    {
        return new SyntaxToken(InternalSyntax.SyntaxFactory.MissingToken(kind, diagnostics), parent: null, position: 0);
    }
}
