// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language.Syntax
{
    internal static class SyntaxFactory
    {
        internal static SyntaxToken Token(SyntaxKind kind, string content, params RazorDiagnostic[] diagnostics)
        {
            return new SyntaxToken(InternalSyntax.SyntaxFactory.Token(kind, content), parent: null, position: 0);
        }
    }
}
