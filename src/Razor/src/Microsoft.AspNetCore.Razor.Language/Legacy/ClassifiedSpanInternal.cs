// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal struct ClassifiedSpanInternal
    {
        public ClassifiedSpanInternal(SourceSpan span, SourceSpan blockSpan, SpanKindInternal spanKind, BlockKindInternal blockKind, AcceptedCharactersInternal acceptedCharacters)
        {
            Span = span;
            BlockSpan = blockSpan;
            SpanKind = spanKind;
            BlockKind = blockKind;
            AcceptedCharacters = acceptedCharacters;
        }

        public AcceptedCharactersInternal AcceptedCharacters { get; }

        public BlockKindInternal BlockKind { get; }

        public SourceSpan BlockSpan { get; }

        public SourceSpan Span { get; }

        public SpanKindInternal SpanKind { get; }
    }
}
