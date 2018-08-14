// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax
{
    internal abstract class HtmlNodeSyntax : GreenNode
    {
        protected HtmlNodeSyntax(SyntaxKind kind)
            : base(kind)
        {
        }

        protected HtmlNodeSyntax(SyntaxKind kind, int fullWidth)
            : base(kind, fullWidth)
        {
        }

        protected HtmlNodeSyntax(SyntaxKind kind, RazorDiagnostic[] diagnostics, SyntaxAnnotation[] annotations)
            : base(kind, diagnostics, annotations)
        {
        }

        internal override GreenNode Accept(SyntaxVisitor visitor)
        {
            return visitor.VisitHtmlNode(this);
        }
    }
}
