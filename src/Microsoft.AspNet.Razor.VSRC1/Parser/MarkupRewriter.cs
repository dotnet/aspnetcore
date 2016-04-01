// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Parser
{
    internal abstract class MarkupRewriter : ParserVisitor, ISyntaxTreeRewriter
    {
        private Stack<BlockBuilder> _blocks = new Stack<BlockBuilder>();
        private Action<SpanBuilder, SourceLocation, string> _markupSpanFactory;

        protected MarkupRewriter(Action<SpanBuilder, SourceLocation, string> markupSpanFactory)
        {
            if (markupSpanFactory == null)
            {
                throw new ArgumentNullException(nameof(markupSpanFactory));
            }

            _markupSpanFactory = markupSpanFactory;
        }

        protected BlockBuilder Parent
        {
            get { return _blocks.Count > 0 ? _blocks.Peek() : null; }
        }

        public virtual void Rewrite(RewritingContext context)
        {
            context.SyntaxTree.Accept(this);
            Debug.Assert(_blocks.Count == 1);
            context.SyntaxTree = _blocks.Pop().Build();
        }

        public override void VisitBlock(Block block)
        {
            if (CanRewrite(block))
            {
                var newNode = RewriteBlock(_blocks.Peek(), block);
                if (newNode != null)
                {
                    _blocks.Peek().Children.Add(newNode);
                }
            }
            else
            {
                // Not rewritable.
                var builder = new BlockBuilder(block);
                builder.Children.Clear();
                _blocks.Push(builder);
                base.VisitBlock(block);
                Debug.Assert(ReferenceEquals(builder, _blocks.Peek()));

                if (_blocks.Count > 1)
                {
                    _blocks.Pop();
                    _blocks.Peek().Children.Add(builder.Build());
                }
            }
        }

        public override void VisitSpan(Span span)
        {
            if (CanRewrite(span))
            {
                var newNode = RewriteSpan(_blocks.Peek(), span);
                if (newNode != null)
                {
                    _blocks.Peek().Children.Add(newNode);
                }
            }
            else
            {
                _blocks.Peek().Children.Add(span);
            }
        }

        protected virtual bool CanRewrite(Block block)
        {
            return false;
        }

        protected virtual bool CanRewrite(Span span)
        {
            return false;
        }

        protected virtual SyntaxTreeNode RewriteBlock(BlockBuilder parent, Block block)
        {
            throw new NotImplementedException();
        }

        protected virtual SyntaxTreeNode RewriteSpan(BlockBuilder parent, Span span)
        {
            throw new NotImplementedException();
        }

        protected void FillSpan(SpanBuilder builder, SourceLocation start, string content)
        {
            _markupSpanFactory(builder, start, content);
        }
    }
}
