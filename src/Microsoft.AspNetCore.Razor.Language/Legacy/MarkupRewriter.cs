// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal abstract class MarkupRewriter : ParserVisitor
    {
        private Stack<BlockBuilder> _blocks;

        protected MarkupRewriter()
        {
            _blocks = new Stack<BlockBuilder>();
        }

        protected BlockBuilder Parent => _blocks.Count > 0 ? _blocks.Peek() : null;

        public Block Rewrite(Block root)
        {
            root.Accept(this);
            Debug.Assert(_blocks.Count == 1);
            var rewrittenRoot = _blocks.Pop().Build();

            return rewrittenRoot;
        }

        public override void VisitBlock(Block block)
        {
            if (CanRewrite(block))
            {
                var newNode = RewriteBlock(Parent, block);
                if (newNode != null)
                {
                    Parent.Children.Add(newNode);
                }
            }
            else
            {
                // Not rewritable.
                var builder = new BlockBuilder(block);
                builder.Children.Clear();
                _blocks.Push(builder);
                base.VisitBlock(block);
                Debug.Assert(ReferenceEquals(builder, Parent));

                if (_blocks.Count > 1)
                {
                    _blocks.Pop();
                    Parent.Children.Add(builder.Build());
                }
            }
        }

        protected abstract bool CanRewrite(Block block);

        protected abstract SyntaxTreeNode RewriteBlock(BlockBuilder parent, Block block);

        public override void VisitSpan(Span span)
        {
            Parent.Children.Add(span);
        }

        protected void FillSpan(SpanBuilder builder, SourceLocation start, string content)
        {
            builder.Kind = SpanKindInternal.Markup;
            builder.ChunkGenerator = new MarkupChunkGenerator();

            foreach (ISymbol sym in HtmlLanguageCharacteristics.Instance.TokenizeString(start, content))
            {
                builder.Accept(sym);
            }
        }
    }
}
