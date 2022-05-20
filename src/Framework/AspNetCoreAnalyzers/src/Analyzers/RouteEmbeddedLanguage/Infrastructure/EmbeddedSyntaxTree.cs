// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.ExternalAccess.AspNetCore.EmbeddedLanguages;

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;

internal abstract class EmbeddedSyntaxTree<TSyntaxKind, TSyntaxNode, TCompilationUnitSyntax>
    where TSyntaxKind : struct
    where TSyntaxNode : EmbeddedSyntaxNode<TSyntaxKind, TSyntaxNode>
    where TCompilationUnitSyntax : TSyntaxNode
{
    public readonly AspNetCoreVirtualCharSequence Text;
    public readonly TCompilationUnitSyntax Root;
    public readonly ImmutableArray<EmbeddedDiagnostic> Diagnostics;

    protected EmbeddedSyntaxTree(
        AspNetCoreVirtualCharSequence text,
        TCompilationUnitSyntax root,
        ImmutableArray<EmbeddedDiagnostic> diagnostics)
    {
        Text = text;
        Root = root;
        Diagnostics = diagnostics;
    }
}
