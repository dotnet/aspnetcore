// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language.Syntax
{
    internal class HtmlTextSyntax : HtmlNodeSyntax
    {
        private SyntaxNode _textTokens;

        internal HtmlTextSyntax(GreenNode green, SyntaxNode parent, int position)
            : base(green, parent, position)
        {
        }

        public SyntaxList<SyntaxNode> TextTokens => new SyntaxList<SyntaxNode>(GetRed(ref _textTokens, 0));

        public string Value => TextTokens[0]?.ToFullString() ?? string.Empty;

        internal override SyntaxNode Accept(SyntaxVisitor visitor)
        {
            return visitor.VisitHtmlText(this);
        }

        internal override SyntaxNode GetCachedSlot(int index)
        {
            switch (index)
            {
                case 0: return _textTokens;
                default: return null;
            }
        }

        internal override SyntaxNode GetNodeSlot(int slot)
        {
            switch (slot)
            {
                case 0: return GetRed(ref _textTokens, 0);
                default: return null;
            }
        }
    }
}
