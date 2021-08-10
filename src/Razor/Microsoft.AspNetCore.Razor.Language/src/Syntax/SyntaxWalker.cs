// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Syntax
{
    /// <summary>
    /// Represents a <see cref="SyntaxVisitor"/> that descends an entire <see cref="SyntaxNode"/> graph
    /// visiting each SyntaxNode and its child SyntaxNodes and <see cref="SyntaxToken"/>s in depth-first order.
    /// </summary>
    internal abstract class SyntaxWalker : SyntaxVisitor
    {
        private int _recursionDepth;

        public override void Visit(SyntaxNode node)
        {
            if (node != null)
            {
                _recursionDepth++;
                StackGuard.EnsureSufficientExecutionStack(_recursionDepth);

                node.Accept(this);

                _recursionDepth--;
            }
        }

        public override void DefaultVisit(SyntaxNode node)
        {
            var children = node.ChildNodes();
            for (var i = 0; i < children.Count; i++)
            {
                var child = children[i];
                Visit(child);
            }
        }

        public override void VisitToken(SyntaxToken token)
        {
            VisitLeadingTrivia(token);
            VisitTrailingTrivia(token);
        }

        public virtual void VisitLeadingTrivia(SyntaxToken token)
        {
            if (token.HasLeadingTrivia)
            {
                foreach (var trivia in token.GetLeadingTrivia())
                {
                    VisitTrivia(trivia);
                }
            }
        }

        public virtual void VisitTrailingTrivia(SyntaxToken token)
        {
            if (token.HasTrailingTrivia)
            {
                foreach (var trivia in token.GetTrailingTrivia())
                {
                    VisitTrivia(trivia);
                }
            }
        }
    }
}
