// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language.Syntax
{
    internal class SyntaxTrivia : SyntaxNode
    {
        internal SyntaxTrivia(GreenNode green, SyntaxNode parent, int position)
            : base(green, parent, position)
        {
        }

        internal new InternalSyntax.SyntaxTrivia Green => (InternalSyntax.SyntaxTrivia)base.Green;

        public string Text => Green.Text;

        internal override sealed SyntaxNode GetCachedSlot(int index)
        {
            throw new InvalidOperationException();
        }

        internal override sealed SyntaxNode GetNodeSlot(int slot)
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
}
