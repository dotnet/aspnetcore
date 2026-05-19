// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.AspNetCore.Analyzers.Infrastructure.VirtualChars;

namespace Microsoft.AspNetCore.Analyzers.Infrastructure.EmbeddedSyntax;

internal abstract class EmbeddedSyntaxTree<TSyntaxKind, TSyntaxNode, TCompilationUnitSyntax>
    where TSyntaxKind : struct
    where TSyntaxNode : EmbeddedSyntaxNode<TSyntaxKind, TSyntaxNode>
    where TCompilationUnitSyntax : TSyntaxNode
{
    public readonly VirtualCharSequence Text;
    public readonly TCompilationUnitSyntax Root;
    public readonly ImmutableArray<EmbeddedDiagnostic> Diagnostics;

    protected EmbeddedSyntaxTree(
        VirtualCharSequence text,
        TCompilationUnitSyntax root,
        ImmutableArray<EmbeddedDiagnostic> diagnostics)
    {
        Text = text;
        Root = root;
        Diagnostics = diagnostics;
    }
}
