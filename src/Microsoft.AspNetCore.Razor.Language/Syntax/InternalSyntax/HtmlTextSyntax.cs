// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax
{
    internal class HtmlTextSyntax : HtmlNodeSyntax
    {
        private readonly GreenNode _value;

        internal HtmlTextSyntax(GreenNode value) : base(SyntaxKind.HtmlText)
        {
            SlotCount = 1;
            _value = value;
            AdjustFlagsAndWidth(value);
        }

        internal HtmlTextSyntax(GreenNode value, RazorDiagnostic[] diagnostics, SyntaxAnnotation[] annotations)
            : base(SyntaxKind.HtmlText, diagnostics, annotations)
        {
            SlotCount = 1;
            _value = value;
            AdjustFlagsAndWidth(value);
        }

        internal SyntaxList<GreenNode> TextTokens => new SyntaxList<GreenNode>(_value);

        internal override GreenNode GetSlot(int index)
        {
            switch (index)
            {
                case 0: return _value;
            }

            throw new InvalidOperationException();
        }

        internal override GreenNode Accept(SyntaxVisitor visitor)
        {
            return visitor.VisitHtmlText(this);
        }

        internal override SyntaxNode CreateRed(SyntaxNode parent, int position)
        {
            return new Syntax.HtmlTextSyntax(this, parent, position);
        }

        internal override GreenNode SetDiagnostics(RazorDiagnostic[] diagnostics)
        {
            return new HtmlTextSyntax(_value, diagnostics, GetAnnotations());
        }

        internal override GreenNode SetAnnotations(SyntaxAnnotation[] annotations)
        {
            return new HtmlTextSyntax(_value, GetDiagnostics(), annotations);
        }
    }
}
