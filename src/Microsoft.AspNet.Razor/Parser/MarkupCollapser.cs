// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Text;

namespace Microsoft.AspNet.Razor.Parser
{
    internal class MarkupCollapser : MarkupRewriter
    {
        public MarkupCollapser(Action<SpanBuilder, SourceLocation, string> markupSpanFactory) : base(markupSpanFactory)
        {
        }

        protected override bool CanRewrite(Span span)
        {
            return span.Kind == SpanKind.Markup && span.CodeGenerator is MarkupCodeGenerator;
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
