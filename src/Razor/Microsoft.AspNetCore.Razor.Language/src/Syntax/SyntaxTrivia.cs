// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal class SyntaxTrivia : SyntaxNode
{
    internal SyntaxTrivia(GreenNode green, SyntaxNode parent, int position)
        : base(green, parent, position)
    {
    }

    internal new InternalSyntax.SyntaxTrivia Green => (InternalSyntax.SyntaxTrivia)base.Green;

    public string Text => Green.Text;

    internal sealed override SyntaxNode GetCachedSlot(int index)
    {
        throw new InvalidOperationException();
    }

    internal sealed override SyntaxNode GetNodeSlot(int slot)
    {
        throw new InvalidOperationException();
    }

    public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
    {
        return visitor.VisitTrivia(this);
    }

    public override void Accept(SyntaxVisitor visitor)
    {
        visitor.VisitTrivia(this);
    }

    public sealed override SyntaxTriviaList GetTrailingTrivia()
    {
        return default(SyntaxTriviaList);
    }

    public sealed override SyntaxTriviaList GetLeadingTrivia()
    {
        return default(SyntaxTriviaList);
    }

    public override string ToString()
    {
        return Text;
    }

    public sealed override string ToFullString()
    {
        return Text;
    }
}
