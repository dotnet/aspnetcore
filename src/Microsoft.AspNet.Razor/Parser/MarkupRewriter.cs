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
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Text;

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
                throw new ArgumentNullException("markupSpanFactory");
            }
            _markupSpanFactory = markupSpanFactory;
        }

        protected BlockBuilder Parent
        {
            get { return _blocks.Count > 0 ? _blocks.Peek() : null; }
        }

        public virtual Block Rewrite(Block input)
        {
            input.Accept(this);
            Debug.Assert(_blocks.Count == 1);
            return _blocks.Pop().Build();
        }

        public override void VisitBlock(Block block)
        {
            if (CanRewrite(block))
            {
                SyntaxTreeNode newNode = RewriteBlock(_blocks.Peek(), block);
                if (newNode != null)
                {
                    _blocks.Peek().Children.Add(newNode);
                }
            }
            else
            {
                // Not rewritable.
                BlockBuilder builder = new BlockBuilder(block);
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
                SyntaxTreeNode newNode = RewriteSpan(_blocks.Peek(), span);
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
