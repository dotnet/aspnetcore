// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Legacy;

namespace Microsoft.AspNetCore.Razor.Language.Syntax
{
    internal class SyntaxToken : SyntaxNode
    {
        internal SyntaxToken(GreenNode green, SyntaxNode parent, int position)
            : this(green, parent, null, position)
        {
        }

        // Temporary plumbing
        internal SyntaxToken(GreenNode green, SyntaxNode parent, Span parentSpan, int position)
            : base(green, parent, position)
        {
            Debug.Assert(parent == null || !parent.Green.IsList, "list cannot be a parent");
            Debug.Assert(green == null || green.IsToken, "green must be a token");

            ParentSpan = parentSpan;
        }

        // Temporary plumbing
        internal Span ParentSpan { get; }

        // Temporary plumbing
        internal SourceLocation Start
        {
            get
            {
                if (ParentSpan == null)
                {
                    return SourceLocation.Undefined;
                }

                var tracker = new SourceLocationTracker(ParentSpan.Start);
                for (var i = 0; i < ParentSpan.Tokens.Count; i++)
                {
                    var token = ParentSpan.Tokens[i];
                    if (object.ReferenceEquals(this, token))
                    {
                        break;
                    }

                    tracker.UpdateLocation(token.Content);
                }

                return tracker.CurrentLocation;
            }
        }

        internal new InternalSyntax.SyntaxToken Green => (InternalSyntax.SyntaxToken)base.Green;

        public string Content => Green.Content;

        internal override sealed SyntaxNode GetCachedSlot(int index)
        {
            throw new InvalidOperationException("Tokens can't have slots.");
        }

        internal override sealed SyntaxNode GetNodeSlot(int slot)
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
            return Content;
        }
    }
}
