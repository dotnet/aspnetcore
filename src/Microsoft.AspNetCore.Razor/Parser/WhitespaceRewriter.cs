// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Parser.Internal;
using Microsoft.AspNetCore.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNetCore.Razor.Parser
{
    internal class WhiteSpaceRewriter : MarkupRewriter
    {
        public WhiteSpaceRewriter(Action<SpanBuilder, SourceLocation, string> markupSpanFactory)
            : base(markupSpanFactory)
        {
            if (markupSpanFactory == null)
            {
                throw new ArgumentNullException(nameof(markupSpanFactory));
            }
        }

        protected override bool CanRewrite(Block block)
        {
            return block.Type == BlockType.Expression && Parent != null;
        }

        protected override SyntaxTreeNode RewriteBlock(BlockBuilder parent, Block block)
        {
            var newBlock = new BlockBuilder(block);
            newBlock.Children.Clear();
            var ws = block.Children.FirstOrDefault() as Span;
            IEnumerable<SyntaxTreeNode> newNodes = block.Children;
            if (ws.Content.All(Char.IsWhiteSpace))
            {
                // Add this node to the parent
                var builder = new SpanBuilder(ws);
                builder.ClearSymbols();
                FillSpan(builder, ws.Start, ws.Content);
                parent.Children.Add(builder.Build());

                // Remove the old whitespace node
                newNodes = block.Children.Skip(1);
            }

            foreach (SyntaxTreeNode node in newNodes)
            {
                newBlock.Children.Add(node);
            }
            return newBlock.Build();
        }
    }
}
