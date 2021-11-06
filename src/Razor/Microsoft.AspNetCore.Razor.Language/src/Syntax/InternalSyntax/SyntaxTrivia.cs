// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;

internal class SyntaxTrivia : GreenNode
{
    internal SyntaxTrivia(SyntaxKind kind, string text)
        : base(kind, text.Length)
    {
        Text = text;
    }

    internal SyntaxTrivia(SyntaxKind kind, string text, RazorDiagnostic[] diagnostics, SyntaxAnnotation[] annotations)
        : base(kind, text.Length, diagnostics, annotations)
    {
        Text = text;
    }

    public string Text { get; }

    internal override bool IsTrivia => true;

    public override int Width => Text.Length;

    protected override void WriteTriviaTo(TextWriter writer)
    {
        writer.Write(Text);
    }

    public sealed override string ToFullString()
    {
        return Text;
    }

    public sealed override int GetLeadingTriviaWidth()
    {
        return 0;
    }

    public sealed override int GetTrailingTriviaWidth()
    {
        return 0;
    }

    protected sealed override int GetSlotCount()
    {
        return 0;
    }

    internal sealed override GreenNode GetSlot(int index)
    {
        throw new InvalidOperationException();
    }

    internal override SyntaxNode CreateRed(SyntaxNode parent, int position)
    {
        return new Syntax.SyntaxTrivia(this, parent, position);
    }

    public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
    {
        return visitor.VisitTrivia(this);
    }

    public override void Accept(SyntaxVisitor visitor)
    {
        visitor.VisitTrivia(this);
    }

    internal override GreenNode SetDiagnostics(RazorDiagnostic[] diagnostics)
    {
        return new SyntaxTrivia(Kind, Text, diagnostics, GetAnnotations());
    }

    internal override GreenNode SetAnnotations(SyntaxAnnotation[] annotations)
    {
        return new SyntaxTrivia(Kind, Text, GetDiagnostics(), annotations);
    }

    public override bool IsEquivalentTo(GreenNode other)
    {
        if (!base.IsEquivalentTo(other))
        {
            return false;
        }

        if (Text != ((SyntaxTrivia)other).Text)
        {
            return false;
        }

        return true;
    }
}
