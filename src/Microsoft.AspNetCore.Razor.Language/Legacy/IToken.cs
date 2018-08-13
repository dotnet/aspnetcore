// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal interface IToken
    {
        Span Parent { get; set; }

        string Content { get; }

        SourceLocation Start { get; }

        SyntaxKind SyntaxKind { get; }

        SyntaxToken SyntaxToken { get; }
    }
}
