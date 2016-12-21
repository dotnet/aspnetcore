// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Text;

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    internal class ConditionalAttributeCollapser : MarkupRewriter
    {
        protected override bool CanRewrite(Block block)
        {
            var generator = block.ChunkGenerator as AttributeBlockChunkGenerator;
            if (generator != null && block.Children.Count > 0)
            {
                // Perf: Avoid allocating an enumerator.
                for (var i = 0; i < block.Children.Count; i++)
                {
                    if (!IsLiteralAttributeValue(block.Children[i]))
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }

        protected override SyntaxTreeNode RewriteBlock(BlockBuilder parent, Block block)
        {
            // Collect the content of this node
            var builder = new StringBuilder();
            for (var i = 0; i < block.Children.Count; i++)
            {
                var childSpan = (Span)block.Children[i];
                builder.Append(childSpan.Content);
            }

            // Create a new span containing this content
            var span = new SpanBuilder(block.Children[0].Start);

            span.EditHandler = SpanEditHandler.CreateDefault(HtmlLanguageCharacteristics.Instance.TokenizeString);
            Debug.Assert(block.Children.Count > 0);
            var start = ((Span)block.Children[0]).Start;
            FillSpan(span, start, builder.ToString());
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

            return span != null &&
                (span.ChunkGenerator is LiteralAttributeChunkGenerator ||
                span.ChunkGenerator is MarkupChunkGenerator ||
                span.ChunkGenerator == SpanChunkGenerator.Null);
        }
    }
}
