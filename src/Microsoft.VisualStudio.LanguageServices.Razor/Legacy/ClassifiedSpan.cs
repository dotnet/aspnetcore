// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    // ----------------------------------------------------------------------------------------------------
    // NOTE: This is only here for VisualStudio binary compatibility. This type should not be used; instead
    // use the Microsoft.CodeAnalysis.Razor variant from Microsoft.CodeAnalysis.Razor.Workspaces
    // ----------------------------------------------------------------------------------------------------
    public struct ClassifiedSpan
    {
        public ClassifiedSpan(SourceSpan span, SourceSpan blockSpan, SpanKind spanKind, BlockKind blockKind, AcceptedCharacters acceptedCharacters)
        {
            Span = span;
            BlockSpan = blockSpan;
            SpanKind = spanKind;
            BlockKind = blockKind;
            AcceptedCharacters = acceptedCharacters;
        }

        public AcceptedCharacters AcceptedCharacters { get; }

        public BlockKind BlockKind { get; }

        public SourceSpan BlockSpan { get; }

        public SourceSpan Span { get; }

        public SpanKind SpanKind { get; }
    }
}
