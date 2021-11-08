// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;

namespace Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;

// Simplified version of Roslyn's SyntaxNodeCache
internal sealed class SyntaxTokenCache
{
    private const int CacheSizeBits = 16;
    private const int CacheSize = 1 << CacheSizeBits;
    private const int CacheMask = CacheSize - 1;
    public static readonly SyntaxTokenCache Instance = new();
    private static readonly Entry[] s_cache = new Entry[CacheSize];

    internal SyntaxTokenCache() { }

    private readonly struct Entry
    {
        public int Hash { get; }
        public SyntaxToken? Token { get; }

        internal Entry(int hash, SyntaxToken token)
        {
            Hash = hash;
            Token = token;
        }
    }

    public bool CanBeCached(SyntaxKind kind, params RazorDiagnostic[] diagnostics)
    {
        if (diagnostics.Length == 0)
        {
            switch (kind)
            {
                case SyntaxKind.CharacterLiteral:
                case SyntaxKind.Dot:
                case SyntaxKind.Identifier:
                case SyntaxKind.IntegerLiteral:
                case SyntaxKind.Keyword:
                case SyntaxKind.NewLine:
                case SyntaxKind.RazorCommentStar:
                case SyntaxKind.RazorCommentTransition:
                case SyntaxKind.StringLiteral:
                case SyntaxKind.Transition:
                case SyntaxKind.Whitespace:
                    return true;
            }
        }

        return false;
    }

    public SyntaxToken GetCachedToken(SyntaxKind kind, string content)
    {
        var hash = (kind, content).GetHashCode();

        // Allow the upper 16 bits to contribute to the index
        var indexableHash = hash ^ (hash >> 16);

        var idx = indexableHash & CacheMask;
        var e = s_cache[idx];

        if (e.Hash == hash && e.Token != null && e.Token.Kind == kind && e.Token.Content == content)
        {
            return e.Token;
        }

        var token = new SyntaxToken(kind, content, Array.Empty<RazorDiagnostic>());
        s_cache[idx] = new Entry(hash, token);

        return token;
    }
}
