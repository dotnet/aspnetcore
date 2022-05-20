// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;

internal static class SyntaxTokenExtensions
{
    public static SyntaxNode? TryFindContainer(this SyntaxToken token)
    {
        var node = WalkUpParentheses(GetRequiredParent(token));

        // if we're inside some collection-like initializer, find the instance actually being created. 
        if (IsAnyInitializerExpression(node.Parent, out var instance))
        {
            node = WalkUpParentheses(instance);
        }

        return node;
    }

    public static SyntaxNode GetRequiredParent(this SyntaxToken token)
        => token.Parent ?? throw new InvalidOperationException("Token's parent was null");

    public static SyntaxNode GetRequiredParent(this SyntaxNode node)
        => node.Parent ?? throw new InvalidOperationException("Node's parent was null");

    [return: NotNullIfNotNull("node")]
    private static SyntaxNode? WalkUpParentheses(SyntaxNode? node)
    {
        while (IsParenthesizedExpression(node?.Parent))
        {
            node = node.Parent;
        }

        return node;
    }

    private static bool IsAnyInitializerExpression([NotNullWhen(true)] SyntaxNode? node, [NotNullWhen(true)] out SyntaxNode? creationExpression)
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

    private static bool IsParenthesizedExpression([NotNullWhen(true)] SyntaxNode? node)
        => node?.RawKind == (int)SyntaxKind.ParenthesizedExpression;

    public static bool IsSimpleAssignmentStatement([NotNullWhen(true)] this SyntaxNode? statement)
        => statement is ExpressionStatementSyntax exprStatement &&
           exprStatement.Expression.IsKind(SyntaxKind.SimpleAssignmentExpression);
}
