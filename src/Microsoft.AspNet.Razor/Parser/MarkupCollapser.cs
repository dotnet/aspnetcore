// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

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
            Span previous = parent.Children.LastOrDefault() as Span;
            if (previous == null || !CanRewrite(previous))
            {
                return span;
            }

            // Merge spans
            parent.Children.Remove(previous);
            SpanBuilder merged = new SpanBuilder();
            FillSpan(merged, previous.Start, previous.Content + span.Content);
            return merged.Build();
        }
    }
}
