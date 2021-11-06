// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;

internal abstract partial class SyntaxVisitor<TResult>
{
    public virtual TResult Visit(GreenNode node)
    {
        if (node == null)
        {
            return default(TResult);
        }

        return node.Accept(this);
    }

    public virtual TResult VisitToken(SyntaxToken token)
    {
        return DefaultVisit(token);
    }

    public virtual TResult VisitTrivia(SyntaxTrivia trivia)
    {
        return DefaultVisit(trivia);
    }

    protected virtual TResult DefaultVisit(GreenNode node)
    {
        return default(TResult);
    }
}

internal abstract partial class SyntaxVisitor
{
    public virtual GreenNode Visit(GreenNode node)
    {
        if (node != null)
        {
            node.Accept(this);
        }

        return null;
    }

    public virtual void VisitToken(SyntaxToken token)
    {
        DefaultVisit(token);
    }

    public virtual void VisitTrivia(SyntaxTrivia trivia)
    {
        DefaultVisit(trivia);
    }

    protected virtual void DefaultVisit(GreenNode node)
    {
    }
}
