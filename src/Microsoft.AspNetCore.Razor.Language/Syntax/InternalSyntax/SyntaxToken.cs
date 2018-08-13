// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax
{
    internal abstract class SyntaxToken : GreenNode
    {
        internal SyntaxToken(SyntaxKind tokenKind, string text, GreenNode leadingTrivia, GreenNode trailingTrivia)
            : base(tokenKind, text.Length)
        {
            Text = text;
            LeadingTrivia = leadingTrivia;
            AdjustFlagsAndWidth(leadingTrivia);
            TrailingTrivia = trailingTrivia;
            AdjustFlagsAndWidth(trailingTrivia);
        }

        internal SyntaxToken(SyntaxKind tokenKind, string text, GreenNode leadingTrivia, GreenNode trailingTrivia, RazorDiagnostic[] diagnostics, SyntaxAnnotation[] annotations)
            : base(tokenKind, text.Length, diagnostics, annotations)
        {
            Text = text;
            LeadingTrivia = leadingTrivia;
            AdjustFlagsAndWidth(leadingTrivia);
            TrailingTrivia = trailingTrivia;
            AdjustFlagsAndWidth(trailingTrivia);
        }

        public string Text { get; }

        public GreenNode LeadingTrivia { get; }

        public GreenNode TrailingTrivia { get; }

        internal override bool IsToken => true;

        public override int Width => Text.Length;

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

            writer.Write(Text);

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

        public abstract SyntaxToken TokenWithLeadingTrivia(GreenNode trivia);

        public sealed override GreenNode WithTrailingTrivia(GreenNode trivia)
        {
            return TokenWithTrailingTrivia(trivia);
        }

        public abstract SyntaxToken TokenWithTrailingTrivia(GreenNode trivia);

        protected override sealed int GetSlotCount()
        {
            return 0;
        }

        internal override sealed GreenNode GetSlot(int index)
        {
            throw new InvalidOperationException();
        }

        internal override GreenNode Accept(SyntaxVisitor visitor)
        {
            return visitor.VisitSyntaxToken(this);
        }

        public override string ToString()
        {
            return Text;
        }
    }
}
