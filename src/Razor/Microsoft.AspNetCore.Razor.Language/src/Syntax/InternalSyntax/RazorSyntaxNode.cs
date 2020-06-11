// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax
{
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
}
