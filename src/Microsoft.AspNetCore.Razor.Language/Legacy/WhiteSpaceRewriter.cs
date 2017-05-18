// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal class WhiteSpaceRewriter : MarkupRewriter
    {
        protected override bool CanRewrite(Block block)
        {
            return block.Type == BlockKindInternal.Expression && Parent != null;
        }

        protected override SyntaxTreeNode RewriteBlock(BlockBuilder parent, Block block)
        {
            var newBlock = new BlockBuilder(block);
            newBlock.Children.Clear();
            var ws = block.Children.FirstOrDefault() as Span;
            IEnumerable<SyntaxTreeNode> newNodes = block.Children;
            if (ws.Content.All(char.IsWhiteSpace))
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
