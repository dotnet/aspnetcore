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
            else if (result.Result != null && _visitedFirstTokenStart && result.Result.Type == CSharpTokenType.NewLine)
            {
                _visitedFirstTokenLineEnd = true;
            }

            return result;
        }

        public override CSharpToken NextToken()
        {
            // Post-Condition: Buffer should be empty at the start of Next()
            Debug.Assert(Buffer.Length == 0);
            StartToken();

            if (EndOfFile || (_visitedFirstTokenStart && _visitedFirstTokenLineEnd))
            {
                return null;
            }

            var token = Turn();

            // Post-Condition: Buffer should be empty at the end of Next()
            Debug.Assert(Buffer.Length == 0);

            return token;
        }

        private bool IsValidTokenType(CSharpTokenType type)
        {
            return type != CSharpTokenType.WhiteSpace &&
                type != CSharpTokenType.NewLine &&
                type != CSharpTokenType.Comment &&
                type != CSharpTokenType.RazorComment &&
                type != CSharpTokenType.RazorCommentStar &&
                type != CSharpTokenType.RazorCommentTransition &&
                type != CSharpTokenType.Transition;
        }
    }
}
