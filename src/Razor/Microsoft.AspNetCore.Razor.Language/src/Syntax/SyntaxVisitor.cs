// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

/// <summary>
/// Represents a <see cref="SyntaxNode"/> visitor that visits only the single SyntaxNode
/// passed into its Visit method and produces
/// a value of the type specified by the <typeparamref name="TResult"/> parameter.
/// </summary>
/// <typeparam name="TResult">
/// The type of the return value this visitor's Visit method.
/// </typeparam>
internal abstract partial class SyntaxVisitor<TResult>
{
    public virtual TResult Visit(SyntaxNode node)
    {
        if (node != null)
        {
            return node.Accept(this);
        }

        return default(TResult);
    }

    public virtual TResult VisitToken(SyntaxToken token)
    {
        return DefaultVisit(token);
    }

    public virtual TResult VisitTrivia(SyntaxTrivia trivia)
    {
        return DefaultVisit(trivia);
    }

    protected virtual TResult DefaultVisit(SyntaxNode node)
    {
        return default(TResult);
    }
}

/// <summary>
/// Represents a <see cref="SyntaxNode"/> visitor that visits only the single SyntaxNode
/// passed into its Visit method.
/// </summary>
internal abstract partial class SyntaxVisitor
{
    public virtual void Visit(SyntaxNode node)
    {
        if (node != null)
        {
            node.Accept(this);
        }
    }

    public virtual void VisitToken(SyntaxToken token)
    {
        DefaultVisit(token);
    }

    public virtual void VisitTrivia(SyntaxTrivia trivia)
    {
        DefaultVisit(trivia);
    }

    public virtual void DefaultVisit(SyntaxNode node)
    {
    }
}
