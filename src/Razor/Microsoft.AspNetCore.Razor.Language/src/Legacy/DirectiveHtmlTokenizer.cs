// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

internal class DirectiveHtmlTokenizer : HtmlTokenizer
{
    private bool _visitedFirstTokenStart;
    private SourceLocation _firstTokenVisitLocation = SourceLocation.Undefined;

    public DirectiveHtmlTokenizer(ITextDocument source) : base(source)
    {
    }

    protected override StateResult Dispatch()
    {
        var location = CurrentLocation;
        var result = base.Dispatch();
        if (result.Result != null && IsValidTokenType(result.Result.Kind))
        {
            _visitedFirstTokenStart = true;
            _firstTokenVisitLocation = location;
        }

        return result;
    }

    public override SyntaxToken NextToken()
    {
        // Post-Condition: Buffer should be empty at the start of Next()
        Debug.Assert(Buffer.Length == 0);
        StartToken();

        if (EndOfFile || (_visitedFirstTokenStart && _firstTokenVisitLocation != CurrentLocation))
        {
            // We also need to make sure we are currently past the position where we found the first token.
            // If the position is equal, that means the parser put the token back for later parsing.
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
            kind != SyntaxKind.RazorCommentLiteral &&
            kind != SyntaxKind.RazorCommentStar &&
            kind != SyntaxKind.RazorCommentTransition &&
            kind != SyntaxKind.Transition;
    }
}
