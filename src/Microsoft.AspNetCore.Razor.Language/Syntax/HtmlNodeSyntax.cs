// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language.Syntax
{
    internal abstract class HtmlNodeSyntax : SyntaxNode
    {
        internal HtmlNodeSyntax(GreenNode green, SyntaxNode parent, int position)
            : base(green, parent, position)
        {
        }

        internal new InternalSyntax.HtmlNodeSyntax Green => (InternalSyntax.HtmlNodeSyntax)base.Green;

        internal override SyntaxNode Accept(SyntaxVisitor visitor)
        {
            return visitor.VisitHtmlNode(this);
        }
    }
}
