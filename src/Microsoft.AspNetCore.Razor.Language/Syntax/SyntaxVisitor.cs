// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language.Syntax
{
    internal abstract class SyntaxVisitor
    {
        public virtual SyntaxNode Visit(SyntaxNode node)
        {
            if (node != null)
            {
                return node.Accept(this);
            }

            return null;
        }

        public virtual SyntaxNode VisitSyntaxNode(SyntaxNode node)
        {
            return node;
        }

        public virtual SyntaxNode VisitHtmlNode(HtmlNodeSyntax node)
        {
            return VisitSyntaxNode(node);
        }

        public virtual SyntaxNode VisitHtmlText(HtmlTextSyntax node)
        {
            return VisitHtmlNode(node);
        }

        public virtual SyntaxToken VisitSyntaxToken(SyntaxToken token)
        {
            return token;
        }

        public virtual SyntaxTrivia VisitSyntaxTrivia(SyntaxTrivia trivia)
        {
            return trivia;
        }
    }
}
