// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Analyzers.Infrastructure.RoutePattern;
using Microsoft.AspNetCore.Analyzers.Infrastructure.VirtualChars;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Analyzers.Infrastructure.EmbeddedSyntax;

internal static class EmbeddedSyntaxHelpers
{
    public static TextSpan GetSpan<TSyntaxKind>(EmbeddedSyntaxToken<TSyntaxKind> token1, EmbeddedSyntaxToken<TSyntaxKind> token2) where TSyntaxKind : struct
    {
        if (token2.VirtualChars.IsEmpty)
        {
            return GetSpan(token1.VirtualChars[0], token1.VirtualChars.Last());
        }
        
        return GetSpan(token1.VirtualChars[0], token2.VirtualChars.Last());
    }

    public static TextSpan GetSpan(VirtualCharSequence virtualChars)
        => GetSpan(virtualChars[0], virtualChars.Last());

    public static TextSpan GetSpan(VirtualChar firstChar, VirtualChar lastChar)
        => TextSpan.FromBounds(firstChar.Span.Start, lastChar.Span.End);

    public static RoutePatternNode? GetChildNode(this RoutePatternNode node, RoutePatternKind kind)
    {
        foreach (var child in node)
        {
            if (child.IsNode && child.Kind == kind)
            {
                return child.Node;
            }
        }

        return null;
    }
}
