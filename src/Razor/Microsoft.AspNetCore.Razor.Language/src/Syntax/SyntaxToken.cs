// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Legacy;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal class SyntaxToken : RazorSyntaxNode
{
    internal SyntaxToken(GreenNode green, SyntaxNode parent, int position)
        : base(green, parent, position)
    {
    }

    internal new InternalSyntax.SyntaxToken Green => (InternalSyntax.SyntaxToken)base.Green;

    public string Content => Green.Content;

    internal sealed override SyntaxNode GetCachedSlot(int index)
    {
        throw new InvalidOperationException("Tokens can't have slots.");
    }

    internal sealed override SyntaxNode GetNodeSlot(int slot)
    {
        throw new InvalidOperationException("Tokens can't have slots.");
    }

    public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
    {
        return visitor.VisitToken(this);
    }

    public override void Accept(SyntaxVisitor visitor)
    {
        visitor.VisitToken(this);
    }

    public SyntaxToken WithLeadingTrivia(SyntaxNode trivia)
    {
        return Green != null
            ? new SyntaxToken(Green.WithLeadingTrivia(trivia.Green), parent: null, position: 0)
            : default(SyntaxToken);
    }

    public SyntaxToken WithTrailingTrivia(SyntaxNode trivia)
    {
        return Green != null
            ? new SyntaxToken(Green.WithTrailingTrivia(trivia.Green), parent: null, position: 0)
            : default(SyntaxToken);
    }

    public SyntaxToken WithLeadingTrivia(IEnumerable<SyntaxTrivia> trivia)
    {
        var greenList = trivia?.Select(t => t.Green);
        return WithLeadingTrivia(Green.CreateList(greenList)?.CreateRed());
    }

    public SyntaxToken WithTrailingTrivia(IEnumerable<SyntaxTrivia> trivia)
    {
        var greenList = trivia?.Select(t => t.Green);
        return WithTrailingTrivia(Green.CreateList(greenList)?.CreateRed());
    }

    public override SyntaxTriviaList GetLeadingTrivia()
    {
        var leading = Green.GetLeadingTrivia();
        if (leading == null)
        {
            return default(SyntaxTriviaList);
        }

        return new SyntaxTriviaList(leading.CreateRed(this, Position), Position);
    }

    public override SyntaxTriviaList GetTrailingTrivia()
    {
        var trailing = Green.GetTrailingTrivia();
        if (trailing == null)
        {
            return default(SyntaxTriviaList);
        }

        var leading = Green.GetLeadingTrivia();
        var index = 0;
        if (leading != null)
        {
            index = leading.IsList ? leading.SlotCount : 1;
        }
        int trailingPosition = Position + FullWidth;
        trailingPosition -= trailing.FullWidth;

        return new SyntaxTriviaList(trailing.CreateRed(this, trailingPosition), trailingPosition, index);
    }

    public override string ToString()
    {
        return Content;
    }
}
