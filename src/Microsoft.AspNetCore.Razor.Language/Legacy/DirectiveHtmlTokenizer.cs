// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal class DirectiveHtmlTokenizer : HtmlTokenizer
    {
        private bool _visitedFirstTokenStart = false;

        public DirectiveHtmlTokenizer(ITextDocument source) : base(source)
        {
        }

        protected override StateResult Dispatch()
        {
            var result = base.Dispatch();
            if (result.Result != null && IsValidTokenType(result.Result.Type))
            {
                _visitedFirstTokenStart = true;
            }

            return result;
        }

        public override HtmlSymbol NextSymbol()
        {
            // Post-Condition: Buffer should be empty at the start of Next()
            Debug.Assert(Buffer.Length == 0);
            StartSymbol();

            if (EndOfFile || _visitedFirstTokenStart)
            {
                return null;
            }

            var symbol = Turn();

            // Post-Condition: Buffer should be empty at the end of Next()
            Debug.Assert(Buffer.Length == 0);

            return symbol;
        }

        private bool IsValidTokenType(HtmlSymbolType type)
        {
            return type != HtmlSymbolType.WhiteSpace &&
                type != HtmlSymbolType.NewLine &&
                type != HtmlSymbolType.RazorComment &&
                type != HtmlSymbolType.RazorCommentStar &&
                type != HtmlSymbolType.RazorCommentTransition &&
                type != HtmlSymbolType.Transition;
        }
    }
}
