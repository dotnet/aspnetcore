// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure.EmbeddedSyntax;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.RoutePattern;

using RoutePatternToken = EmbeddedSyntaxToken<RoutePatternKind>;

internal static class RoutePatternHelpers
{
    public static RoutePatternToken CreateToken(RoutePatternKind kind, VirtualCharSequence virtualChars)
        => new(kind, virtualChars, ImmutableArray<EmbeddedDiagnostic>.Empty, value: null);

    public static RoutePatternToken CreateMissingToken(RoutePatternKind kind)
        => CreateToken(kind, VirtualCharSequence.Empty);

    public static bool TryGetNode<TSyntaxKind, TSyntaxNode>(this EmbeddedSyntaxNodeOrToken<TSyntaxKind, TSyntaxNode> nodeOrToken, TSyntaxKind kind, [NotNullWhen(true)] out TSyntaxNode? node)
        where TSyntaxKind : struct
        where TSyntaxNode : EmbeddedSyntaxNode<TSyntaxKind, TSyntaxNode>
    {
        if (Equals(nodeOrToken.Kind, kind))
        {
            node = nodeOrToken.Node!;
            return true;
        }

        node = null;
        return false;
    }
}
