// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax
{
    // Simplified version of Roslyn's SyntaxNodeCache
    internal static class WhitespaceTokenCache
    {
        private const int CacheSizeBits = 8;
        private const int CacheSize = 1 << CacheSizeBits;
        private const int CacheMask = CacheSize - 1;
        private static readonly Entry[] s_cache = new Entry[CacheSize];

        private struct Entry
        {
            public int Hash { get; }
            public SyntaxToken Token { get; }

            internal Entry(int hash, SyntaxToken token)
            {
                Hash = hash;
                Token = token;
            }
        }

        public static SyntaxToken GetToken(string content)
        {
            var hash = content.GetHashCode();

            var idx = hash & CacheMask;
            var e = s_cache[idx];

            if (e.Hash == hash && e.Token?.Content == content)
            {
                return e.Token;
            }

            var token = new SyntaxToken(SyntaxKind.Whitespace, content, Array.Empty<RazorDiagnostic>());
            s_cache[idx] = new Entry(hash, token);

            return token;
        }
    }
}
