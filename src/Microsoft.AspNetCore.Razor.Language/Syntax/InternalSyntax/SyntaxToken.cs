// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax
{
    internal class SyntaxToken : RazorSyntaxNode
    {
        private readonly GreenNode _leadingTrivia;
        private readonly GreenNode _trailingTrivia;

        internal SyntaxToken(SyntaxKind kind, string content, RazorDiagnostic[] diagnostics)
            : base(kind, content.Length, diagnostics, annotations: null)
        {
            Content = content;
        }

        internal SyntaxToken(SyntaxKind kind, string content, GreenNode leadingTrivia, GreenNode trailingTrivia)
            : base(kind, content.Length)
        {
            Content = content;
            _leadingTrivia = leadingTrivia;
            AdjustFlagsAndWidth(leadingTrivia);
            _trailingTrivia = trailingTrivia;
            AdjustFlagsAndWidth(trailingTrivia);
        }

        internal SyntaxToken(SyntaxKind kind, string content, GreenNode leadingTrivia, GreenNode trailingTrivia, RazorDiagnostic[] diagnostics, SyntaxAnnotation[] annotations)
            : base(kind, content.Length, diagnostics, annotations)
        {
            Content = content;
            _leadingTrivia = leadingTrivia;
            AdjustFlagsAndWidth(leadingTrivia);
            _trailingTrivia = trailingTrivia;
            AdjustFlagsAndWidth(trailingTrivia);
        }

        public string Content { get; }

        public SyntaxList<GreenNode> LeadingTrivia
        {
            get { return new SyntaxList<GreenNode>(GetLeadingTrivia()); }
        }

        public SyntaxList<GreenNode> TrailingTrivia
        {
            get { return new SyntaxList<GreenNode>(GetTrailingTrivia()); }
        }

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
            return _leadingTrivia;
        }

        public override int GetLeadingTriviaWidth()
        {
            return _leadingTrivia == null ? 0 : _leadingTrivia.FullWidth;
        }

        public override sealed GreenNode GetTrailingTrivia()
        {
            return _trailingTrivia;
        }

        public override int GetTrailingTriviaWidth()
        {
            return _trailingTrivia == null ? 0 : _trailingTrivia.FullWidth;
        }

        public sealed override GreenNode WithLeadingTrivia(GreenNode trivia)
        {
            return TokenWithLeadingTrivia(trivia);
        }

        public virtual SyntaxToken TokenWithLeadingTrivia(GreenNode trivia)
        {
            return new SyntaxToken(Kind, Content, trivia, _trailingTrivia, GetDiagnostics(), GetAnnotations());
        }

        public sealed override GreenNode WithTrailingTrivia(GreenNode trivia)
        {
            return TokenWithTrailingTrivia(trivia);
        }

        public virtual SyntaxToken TokenWithTrailingTrivia(GreenNode trivia)
        {
            return new SyntaxToken(Kind, Content, _leadingTrivia, trivia, GetDiagnostics(), GetAnnotations());
        }

        internal override GreenNode SetDiagnostics(RazorDiagnostic[] diagnostics)
        {
            return new SyntaxToken(Kind, Content, _leadingTrivia, _trailingTrivia, diagnostics, GetAnnotations());
        }

        internal override GreenNode SetAnnotations(SyntaxAnnotation[] annotations)
        {
            return new SyntaxToken(Kind, Content, _leadingTrivia, _trailingTrivia, GetDiagnostics(), annotations);
        }

        protected override sealed int GetSlotCount()
        {
            return 0;
        }

        internal override sealed GreenNode GetSlot(int index)
        {
            throw new InvalidOperationException("Tokens don't have slots.");
        }

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
        {
            return visitor.VisitToken(this);
        }

        public override void Accept(SyntaxVisitor visitor)
        {
            visitor.VisitToken(this);
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

        internal static SyntaxToken CreateMissing(SyntaxKind kind, params RazorDiagnostic[] diagnostics)
        {
            return new MissingToken(kind, diagnostics);
        }

        private class MissingToken : SyntaxToken
        {
            internal MissingToken(SyntaxKind kind, RazorDiagnostic[] diagnostics)
                : base(kind, string.Empty, diagnostics)
            {
                Flags |= NodeFlags.IsMissing;
            }

            internal MissingToken(SyntaxKind kind, GreenNode leading, GreenNode trailing, RazorDiagnostic[] diagnostics, SyntaxAnnotation[] annotations)
                : base(kind, string.Empty, leading, trailing, diagnostics, annotations)
            {
                Flags |= NodeFlags.IsMissing;
            }
        }
    }
}
