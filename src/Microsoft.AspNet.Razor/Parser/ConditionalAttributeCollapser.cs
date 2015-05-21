// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNet.Razor.Editor;
using Microsoft.AspNet.Razor.Chunks.Generators;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Tokenizer;

namespace Microsoft.AspNet.Razor.Parser
{
    internal class ConditionalAttributeCollapser : MarkupRewriter
    {
        public ConditionalAttributeCollapser(Action<SpanBuilder, SourceLocation, string> markupSpanFactory) : base(markupSpanFactory)
        {
        }

        protected override bool CanRewrite(Block block)
        {
            var gen = block.ChunkGenerator as AttributeBlockChunkGenerator;
            return gen != null && block.Children.Any() && block.Children.All(IsLiteralAttributeValue);
        }

        protected override SyntaxTreeNode RewriteBlock(BlockBuilder parent, Block block)
        {
            // Collect the content of this node
            var content = string.Concat(block.Children.Cast<Span>().Select(s => s.Content));

            // Create a new span containing this content
            var span = new SpanBuilder();
            span.EditHandler = new SpanEditHandler(HtmlTokenizer.Tokenize);
            FillSpan(span, block.Children.Cast<Span>().First().Start, content);
            return span.Build();
        }

        private bool IsLiteralAttributeValue(SyntaxTreeNode node)
        {
            if (node.IsBlock)
            {
                return false;
            }
            var span = node as Span;
            Debug.Assert(span != null);

            var litGen = span.ChunkGenerator as LiteralAttributeChunkGenerator;

            return span != null &&
                   ((litGen != null && litGen.ValueGenerator == null) ||
                    span.ChunkGenerator == SpanChunkGenerator.Null ||
                    span.ChunkGenerator is MarkupChunkGenerator);
        }
    }
}
