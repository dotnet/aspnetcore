// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;

internal abstract partial class RazorSyntaxNode : GreenNode
{
    protected RazorSyntaxNode(SyntaxKind kind) : base(kind)
    {
    }

    protected RazorSyntaxNode(SyntaxKind kind, int fullWidth)
        : base(kind, fullWidth)
    {
    }

    protected RazorSyntaxNode(SyntaxKind kind, RazorDiagnostic[] diagnostics, SyntaxAnnotation[] annotations)
        : base(kind, diagnostics, annotations)
    {
    }

    protected RazorSyntaxNode(SyntaxKind kind, int fullWidth, RazorDiagnostic[] diagnostics, SyntaxAnnotation[] annotations)
        : base(kind, fullWidth, diagnostics, annotations)
    {
    }
}
