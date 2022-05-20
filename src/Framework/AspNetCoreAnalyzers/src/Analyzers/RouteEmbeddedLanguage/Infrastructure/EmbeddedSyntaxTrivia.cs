// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.ExternalAccess.AspNetCore.EmbeddedLanguages;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;

/// <summary>
/// Trivia on an <see cref="EmbeddedSyntaxToken{TSyntaxKind}"/>.
/// </summary>
internal struct EmbeddedSyntaxTrivia<TSyntaxKind> where TSyntaxKind : struct
{
    public readonly TSyntaxKind Kind;
    public readonly AspNetCoreVirtualCharSequence VirtualChars;

    /// <summary>
    /// A place for diagnostics to be stored during parsing.  Not intended to be accessed 
    /// directly.  These will be collected and aggregated into <see cref="EmbeddedSyntaxTree{TNode, TRoot, TSyntaxKind}.Diagnostics"/>
    /// </summary> 
    internal readonly ImmutableArray<EmbeddedDiagnostic> Diagnostics;

    public EmbeddedSyntaxTrivia(TSyntaxKind kind, AspNetCoreVirtualCharSequence virtualChars, ImmutableArray<EmbeddedDiagnostic> diagnostics)
    {
        Debug.Assert(virtualChars.Length > 0);
        Kind = kind;
        VirtualChars = virtualChars;
        Diagnostics = diagnostics;
    }

    public TextSpan GetSpan()
        => EmbeddedSyntaxHelpers.GetSpan(VirtualChars);

    public override string ToString()
        => VirtualChars.CreateString();
}
