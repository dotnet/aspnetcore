// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Razor.Chunks.Generators;
using Microsoft.AspNetCore.Razor.Parser.Internal;
using Microsoft.AspNetCore.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNetCore.Razor.Parser
{
    internal class MarkupCollapser : MarkupRewriter
    {
        public MarkupCollapser(Action<SpanBuilder, SourceLocation, string> markupSpanFactory) : base(markupSpanFactory)
        {
        }

        protected override bool CanRewrite(Span span)
        {
            return span.Kind == SpanKind.Markup && span.ChunkGenerator is MarkupChunkGenerator;
        }

        protected override SyntaxTreeNode RewriteSpan(BlockBuilder parent, Span span)
        {
            // Only rewrite if we have a previous that is also markup (CanRewrite does this check for us!)
            var previous = parent.Children.LastOrDefault() as Span;
            if (previous == null || !CanRewrite(previous))
            {
                return span;
            }

            // Merge spans
            parent.Children.Remove(previous);
            var merged = new SpanBuilder();
            FillSpan(merged, previous.Start, previous.Content + span.Content);
            return merged.Build();
        }
    }
}
