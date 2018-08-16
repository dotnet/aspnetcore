// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Razor.Language.Legacy;

namespace Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax
{
    internal class SyntaxToken : GreenNode
    {
        internal SyntaxToken(SyntaxKind kind, string content, RazorDiagnostic[] diagnostics)
            : base(kind, content.Length, diagnostics, annotations: null)
        {
            Content = content;
        }

        internal SyntaxToken(SyntaxKind kind, string content, GreenNode leadingTrivia, GreenNode trailingTrivia)
            : base(kind, content.Length)
        {
            Content = content;
            LeadingTrivia = leadingTrivia;
            AdjustFlagsAndWidth(leadingTrivia);
            TrailingTrivia = trailingTrivia;
            AdjustFlagsAndWidth(trailingTrivia);
        }

        internal SyntaxToken(SyntaxKind kind, string content, GreenNode leadingTrivia, GreenNode trailingTrivia, RazorDiagnostic[] diagnostics, SyntaxAnnotation[] annotations)
            : base(kind, content.Length, diagnostics, annotations)
        {
            Content = content;
            LeadingTrivia = leadingTrivia;
            AdjustFlagsAndWidth(leadingTrivia);
            TrailingTrivia = trailingTrivia;
            AdjustFlagsAndWidth(trailingTrivia);
        }

        public string Content { get; }

        public GreenNode LeadingTrivia { get; }

        public GreenNode TrailingTrivia { get; }

        internal override bool IsToken => true;

        public override int Width => Content.Length;

        internal override SyntaxNode CreateRed(SyntaxNode parent, int position)
        {
            return new Syntax.SyntaxToken(this, parent, position);
        }

        protected override void WriteTokenTo(TextWriter writer, bool leading, bool trailing)
        {
            if (leading)
            {
                var trivia = GetLeadingTrivia();
                if (trivia != null)
                {
                    trivia.WriteTo(writer, true, true);
                }
            }

            writer.Write(Content);

            if (trailing)
            {
                var trivia = GetTrailingTrivia();
                if (trivia != null)
                {
                    trivia.WriteTo(writer, true, true);
                }
            }
        }

        public override sealed GreenNode GetLeadingTrivia()
        {
            return LeadingTrivia;
        }

        public override int GetLeadingTriviaWidth()
        {
            return LeadingTrivia == null ? 0 : LeadingTrivia.FullWidth;
        }

        public override sealed GreenNode GetTrailingTrivia()
        {
            return TrailingTrivia;
        }

        public override int GetTrailingTriviaWidth()
        {
            return TrailingTrivia == null ? 0 : TrailingTrivia.FullWidth;
        }

        public sealed override GreenNode WithLeadingTrivia(GreenNode trivia)
        {
            return TokenWithLeadingTrivia(trivia);
        }

        public virtual SyntaxToken TokenWithLeadingTrivia(GreenNode trivia)
        {
            return new SyntaxToken(Kind, Content, trivia, TrailingTrivia, GetDiagnostics(), GetAnnotations());
        }

        public sealed override GreenNode WithTrailingTrivia(GreenNode trivia)
        {
            return TokenWithTrailingTrivia(trivia);
        }

        public virtual SyntaxToken TokenWithTrailingTrivia(GreenNode trivia)
        {
            return new SyntaxToken(Kind, Content, LeadingTrivia, trivia, GetDiagnostics(), GetAnnotations());
        }

        internal override GreenNode SetDiagnostics(RazorDiagnostic[] diagnostics)
        {
            return new SyntaxToken(Kind, Content, LeadingTrivia, TrailingTrivia, diagnostics, GetAnnotations());
        }

        internal override GreenNode SetAnnotations(SyntaxAnnotation[] annotations)
        {
            return new SyntaxToken(Kind, Content, LeadingTrivia, TrailingTrivia, GetDiagnostics(), annotations);
        }

        protected override sealed int GetSlotCount()
        {
            return 0;
        }

        internal override sealed GreenNode GetSlot(int index)
        {
            throw new InvalidOperationException("Tokens don't have slots.");
        }

        internal override GreenNode Accept(SyntaxVisitor visitor)
        {
            return visitor.VisitSyntaxToken(this);
        }

        public override bool IsEquivalentTo(GreenNode other)
        {
            if (!base.IsEquivalentTo(other))
            {
                return false;
            }

            var otherToken = (SyntaxToken)other;

            if (Content != otherToken.Content)
            {
                return false;
            }

            var thisLeading = GetLeadingTrivia();
            var otherLeading = otherToken.GetLeadingTrivia();
            if (thisLeading != otherLeading)
            {
                if (thisLeading == null || otherLeading == null)
                {
                    return false;
                }

                if (!thisLeading.IsEquivalentTo(otherLeading))
                {
                    return false;
                }
            }

            var thisTrailing = GetTrailingTrivia();
            var otherTrailing = otherToken.GetTrailingTrivia();
            if (thisTrailing != otherTrailing)
            {
                if (thisTrailing == null || otherTrailing == null)
                {
                    return false;
                }

                if (!thisTrailing.IsEquivalentTo(otherTrailing))
                {
                    return false;
                }
            }

            return true;
        }

        public override string ToString()
        {
            return Content;
        }
    }
}
