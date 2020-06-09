// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;

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
            if (result.Result != null && !_visitedFirstTokenStart && IsValidTokenType(result.Result.Kind))
            {
                _visitedFirstTokenStart = true;
            }
            else if (result.Result != null && _visitedFirstTokenStart && result.Result.Kind == SyntaxKind.NewLine)
            {
                _visitedFirstTokenLineEnd = true;
            }

            return result;
        }

        public override SyntaxToken NextToken()
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

        private bool IsValidTokenType(SyntaxKind kind)
        {
            return kind != SyntaxKind.Whitespace &&
                kind != SyntaxKind.NewLine &&
                kind != SyntaxKind.CSharpComment &&
                kind != SyntaxKind.RazorCommentLiteral &&
                kind != SyntaxKind.RazorCommentStar &&
                kind != SyntaxKind.RazorCommentTransition &&
                kind != SyntaxKind.Transition;
        }
    }
}
