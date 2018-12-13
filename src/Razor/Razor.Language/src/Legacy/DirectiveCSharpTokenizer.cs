// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal class DirectiveCSharpTokenizer : CSharpTokenizer
    {
        private bool _visitedFirstTokenStart = false;
        private bool _visitedFirstTokenLineEnd = false;

        public DirectiveCSharpTokenizer(ITextDocument source) : base(source)
        {
        }

        protected override StateResult Dispatch()
        {
            var result = base.Dispatch();
            if (result.Result != null && !_visitedFirstTokenStart && IsValidTokenType(result.Result.Type))
            {
                _visitedFirstTokenStart = true;
            }
            else if (result.Result != null && _visitedFirstTokenStart && result.Result.Type == CSharpSymbolType.NewLine)
            {
                _visitedFirstTokenLineEnd = true;
            }

            return result;
        }

        public override CSharpSymbol NextSymbol()
        {
            // Post-Condition: Buffer should be empty at the start of Next()
            Debug.Assert(Buffer.Length == 0);
            StartSymbol();

            if (EndOfFile || (_visitedFirstTokenStart && _visitedFirstTokenLineEnd))
            {
                return null;
            }

            var symbol = Turn();

            // Post-Condition: Buffer should be empty at the end of Next()
            Debug.Assert(Buffer.Length == 0);

            return symbol;
        }

        private bool IsValidTokenType(CSharpSymbolType type)
        {
            return type != CSharpSymbolType.WhiteSpace &&
                type != CSharpSymbolType.NewLine &&
                type != CSharpSymbolType.Comment &&
                type != CSharpSymbolType.RazorComment &&
                type != CSharpSymbolType.RazorCommentStar &&
                type != CSharpSymbolType.RazorCommentTransition &&
                type != CSharpSymbolType.Transition;
        }
    }
}
