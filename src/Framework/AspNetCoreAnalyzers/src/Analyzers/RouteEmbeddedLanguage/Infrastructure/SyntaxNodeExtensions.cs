// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;

internal static class SyntaxNodeExtensions
{
    /// <summary>
    /// Look inside a trivia list for a skipped token that contains the given position.
    /// </summary>
    private static readonly Func<SyntaxTriviaList, int, SyntaxToken> FindSkippedTokenBackwardFunc = FindSkippedTokenBackward;

    public static SyntaxNode GetRequiredParent(this SyntaxNode node)
        => node.Parent ?? throw new InvalidOperationException("Node's parent was null");

    public static SyntaxNode? GetParent(this SyntaxNode node, bool ascendOutOfTrivia)
    {
        var parent = node.Parent;
        if (parent == null && ascendOutOfTrivia)
        {
            if (node is IStructuredTriviaSyntax structuredTrivia)
            {
                parent = structuredTrivia.ParentTrivia.Token.Parent;
            }
        }

        return parent;
    }

    public static bool IsLiteralExpression([NotNullWhen(true)] this SyntaxNode? node)
        => node is LiteralExpressionSyntax;

    public static bool IsBinaryExpression([NotNullWhen(true)] this SyntaxNode? node)
        => node is BinaryExpressionSyntax;

    [return: NotNullIfNotNull("node")]
    public static SyntaxNode? WalkUpParentheses(this SyntaxNode? node)
    {
        while (node?.Parent?.RawKind == (int)SyntaxKind.ParenthesizedExpression)
        {
            node = node.Parent;
        }

        return node;
    }

    public static bool IsAnyInitializerExpression([NotNullWhen(true)] this SyntaxNode? node, [NotNullWhen(true)] out SyntaxNode? creationExpression)
    {
        if (node is InitializerExpressionSyntax
            {
                Parent: BaseObjectCreationExpressionSyntax or ArrayCreationExpressionSyntax or ImplicitArrayCreationExpressionSyntax
            })
        {
            creationExpression = node.Parent;
            return true;
        }

        creationExpression = null;
        return false;
    }

    public static bool IsSimpleAssignmentStatement([NotNullWhen(true)] this SyntaxNode? statement)
        => statement is ExpressionStatementSyntax exprStatement &&
           exprStatement.Expression.IsKind(SyntaxKind.SimpleAssignmentExpression);

    /// <summary>
    /// If the position is inside of token, return that token; otherwise, return the token to the left.
    /// </summary>
    public static SyntaxToken FindTokenOnLeftOfPosition(
        this SyntaxNode root,
        int position,
        bool includeSkipped = false,
        bool includeDirectives = false,
        bool includeDocumentationComments = false)
    {
        var findSkippedToken = includeSkipped ? FindSkippedTokenBackwardFunc : ((l, p) => default);

        var token = GetInitialToken(root, position, includeSkipped, includeDirectives, includeDocumentationComments);

        if (position <= token.SpanStart)
        {
            do
            {
                var skippedToken = findSkippedToken(token.LeadingTrivia, position);
                token = skippedToken.RawKind != 0
                    ? skippedToken
                    : token.GetPreviousToken(includeZeroWidth: false, includeSkipped: includeSkipped, includeDirectives: includeDirectives, includeDocumentationComments: includeDocumentationComments);
            }
            while (position <= token.SpanStart && root.FullSpan.Start < token.SpanStart);
        }
        else if (token.Span.End < position)
        {
            var skippedToken = findSkippedToken(token.TrailingTrivia, position);
            token = skippedToken.RawKind != 0 ? skippedToken : token;
        }

        if (token.Span.Length == 0)
        {
            token = token.GetPreviousToken();
        }

        return token;
    }

    private static SyntaxToken GetInitialToken(
        SyntaxNode root,
        int position,
        bool includeSkipped = false,
        bool includeDirectives = false,
        bool includeDocumentationComments = false)
    {
        return position < root.FullSpan.End || !(root is ICompilationUnitSyntax)
            ? root.FindToken(position, includeSkipped || includeDirectives || includeDocumentationComments)
            : root.GetLastToken(includeZeroWidth: true, includeSkipped: true, includeDirectives: true, includeDocumentationComments: true)
                  .GetPreviousToken(includeZeroWidth: false, includeSkipped: includeSkipped, includeDirectives: includeDirectives, includeDocumentationComments: includeDocumentationComments);
    }

    /// <summary>
    /// Look inside a trivia list for a skipped token that contains the given position.
    /// </summary>
    private static SyntaxToken FindSkippedTokenBackward(SyntaxTriviaList triviaList, int position)
    {
        foreach (var trivia in Enumerable.Reverse(triviaList))
        {
            if (trivia.HasStructure)
            {
                if (trivia.GetStructure() is ISkippedTokensTriviaSyntax skippedTokensTrivia)
                {
                    foreach (var token in skippedTokensTrivia.Tokens)
                    {
                        if (token.Span.Length > 0 && token.SpanStart <= position)
                        {
                            return token;
                        }
                    }
                }
            }
        }

        return default;
    }
}
