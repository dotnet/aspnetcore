// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;

internal struct EmbeddedSyntaxNodeOrToken<TSyntaxKind, TSyntaxNode>
    where TSyntaxKind : struct
    where TSyntaxNode : EmbeddedSyntaxNode<TSyntaxKind, TSyntaxNode>
{
    private readonly EmbeddedSyntaxToken<TSyntaxKind> _token;

    public readonly TSyntaxNode? Node;

    private EmbeddedSyntaxNodeOrToken(TSyntaxNode? node) : this()
    {
        Node = node;
    }

    private EmbeddedSyntaxNodeOrToken(EmbeddedSyntaxToken<TSyntaxKind> token) : this()
    {
        Debug.Assert((int)(object)token.Kind != 0);
        _token = token;
    }

    public readonly EmbeddedSyntaxToken<TSyntaxKind> Token
    {
        get
        {
            Debug.Assert(Node == null);
            return _token;
        }
    }
    public TSyntaxKind Kind => Node?.Kind ?? Token.Kind;

    [MemberNotNullWhen(true, nameof(Node))]
    public bool IsNode => Node != null;

    public TextSpan? GetFullSpan()
        => IsNode ? Node.GetFullSpan() : _token.GetFullSpan();

    public static implicit operator EmbeddedSyntaxNodeOrToken<TSyntaxKind, TSyntaxNode>(TSyntaxNode? node)
        => new(node);

    public static implicit operator EmbeddedSyntaxNodeOrToken<TSyntaxKind, TSyntaxNode>(EmbeddedSyntaxToken<TSyntaxKind> token)
        => new(token);
}
