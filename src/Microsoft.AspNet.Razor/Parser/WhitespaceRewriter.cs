// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Razor.Parser
{
    internal class WhiteSpaceRewriter : MarkupRewriter
    {
        public WhiteSpaceRewriter([NotNull] Action<SpanBuilder, SourceLocation, string> markupSpanFactory)
            : base(markupSpanFactory)
        {
        }

        protected override bool CanRewrite(Block block)
        {
            return block.Type == BlockType.Expression && Parent != null;
        }

        //public override void VisitBlock(Block block)
        //{
        //    BlockBuilder parent = null;
        //    if (_blocks.Count > 0)
        //    {
        //        parent = _blocks.Peek();
        //    }
        //    BlockBuilder newBlock = new BlockBuilder(block);
        //    newBlock.Children.Clear();
        //    _blocks.Push(newBlock);
        //    if (block.Type == BlockType.Expression && parent != null)
        //    {
        //        VisitExpressionBlock(block, parent);
        //    }
        //    else
        //    {
        //        base.VisitBlock(block);
        //    }
        //    if (_blocks.Count > 1)
        //    {
        //        parent.Children.Add(_blocks.Pop().Build());
        //    }
        //}

        //public override void VisitSpan(Span span)
        //{
        //    Debug.Assert(_blocks.Count > 0);
        //    _blocks.Peek().Children.Add(span);
        //}

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
