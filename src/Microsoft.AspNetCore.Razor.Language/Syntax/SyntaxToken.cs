// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language.Syntax
{
    internal abstract class SyntaxToken : SyntaxNode
    {
        internal SyntaxToken(GreenNode green, SyntaxNode parent, int position)
            : base(green, parent, position)
        {
        }

        internal new InternalSyntax.SyntaxToken Green => (InternalSyntax.SyntaxToken)base.Green;

        public string Text => Green.Text;

        internal override sealed SyntaxNode GetCachedSlot(int index)
        {
            throw new InvalidOperationException();
        }

        internal override sealed SyntaxNode GetNodeSlot(int slot)
        {
            throw new InvalidOperationException();
        }

        internal override SyntaxNode Accept(SyntaxVisitor visitor)
        {
            return visitor.VisitSyntaxToken(this);
        }

        internal abstract SyntaxToken WithLeadingTriviaCore(SyntaxNode trivia);

        internal abstract SyntaxToken WithTrailingTriviaCore(SyntaxNode trivia);

        public SyntaxToken WithLeadingTrivia(SyntaxNode trivia) => WithLeadingTriviaCore(trivia);

        public SyntaxToken WithTrailingTrivia(SyntaxNode trivia) => WithTrailingTriviaCore(trivia);

        public SyntaxToken WithLeadingTrivia(IEnumerable<SyntaxTrivia> trivia)
        {
            var greenList = trivia?.Select(t => t.Green);
            return WithLeadingTriviaCore(Green.CreateList(greenList)?.CreateRed());
        }

        public SyntaxToken WithTrailingTrivia(IEnumerable<SyntaxTrivia> trivia)
        {
            var greenList = trivia?.Select(t => t.Green);
            return WithTrailingTriviaCore(Green.CreateList(greenList)?.CreateRed());
        }

        public override SyntaxTriviaList GetLeadingTrivia()
        {
            if (Green.LeadingTrivia == null)
            {
                return default(SyntaxTriviaList);
            }

            return new SyntaxTriviaList(Green.LeadingTrivia.CreateRed(this, Position), Position);
        }

        public override SyntaxTriviaList GetTrailingTrivia()
        {
            var trailingGreen = Green.TrailingTrivia;
            if (trailingGreen == null)
            {
                return default(SyntaxTriviaList);
            }

            var leading = Green.LeadingTrivia;
            int index = 0;
            if (leading != null)
            {
                index = leading.IsList ? leading.SlotCount : 1;
            }
            int trailingPosition = Position + FullWidth;
            trailingPosition -= trailingGreen.FullWidth;

            return new SyntaxTriviaList(trailingGreen.CreateRed(this, trailingPosition), trailingPosition, index);
        }

        public override string ToString()
        {
            return Text;
        }
    }
}
