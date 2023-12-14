// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;

internal static class SyntaxTokenExtensions
{
    public static SyntaxNode? TryFindContainer(this SyntaxToken token)
    {
        var node = token.GetRequiredParent().WalkUpParentheses();

        // if we're inside some collection-like initializer, find the instance actually being created. 
        if (node.Parent.IsAnyInitializerExpression(out var instance))
        {
            node = instance.WalkUpParentheses();
        }

        return node;
    }

    public static SyntaxNode GetRequiredParent(this SyntaxToken token)
        => token.Parent ?? throw new InvalidOperationException("Token's parent was null");
}
