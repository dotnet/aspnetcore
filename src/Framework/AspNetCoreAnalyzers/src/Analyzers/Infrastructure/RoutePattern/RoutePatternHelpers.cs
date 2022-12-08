// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Analyzers.Infrastructure.EmbeddedSyntax;
using Microsoft.AspNetCore.Analyzers.Infrastructure.VirtualChars;

namespace Microsoft.AspNetCore.Analyzers.Infrastructure.RoutePattern;

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
        // Use EqualityComparer.Equals instead of object.Equals to avoid boxing.
        if (EqualityComparer<TSyntaxKind>.Default.Equals(nodeOrToken.Kind, kind))
        {
            // Caller is specifying the kind so should know that the kind is for a node. Assert to double check.
            AnalyzerDebug.Assert(nodeOrToken.Node != null);

            node = nodeOrToken.Node;
            return true;
        }

        node = null;
        return false;
    }
}
