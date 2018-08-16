// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax
{
    internal static class SyntaxFactory
    {
        internal static HtmlTextSyntax HtmlText(SyntaxList<SyntaxToken> textTokens)
        {
            return new HtmlTextSyntax(textTokens.Node);
        }

        internal static SyntaxToken Token(SyntaxKind kind, string content, IEnumerable<RazorDiagnostic> diagnostics)
        {
            return Token(kind, content, diagnostics.ToArray());
        }

        internal static SyntaxToken Token(SyntaxKind kind, string content, params RazorDiagnostic[] diagnostics)
        {
            return new SyntaxToken(kind, content, diagnostics);
        }
    }
}
