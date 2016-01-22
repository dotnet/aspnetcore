// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Parser
{
    public abstract class ParserVisitor
    {
        public CancellationToken? CancelToken { get; set; }

        public virtual void VisitBlock(Block block)
        {
            VisitStartBlock(block);

            // Perf: Avoid allocating an enumerator.
            for (var i = 0; i < block.Children.Count; i++)
            {
                block.Children[i].Accept(this);
            }
            VisitEndBlock(block);
        }

        public virtual void VisitStartBlock(Block block)
        {
            ThrowIfCanceled();
        }

        public virtual void VisitSpan(Span span)
        {
            ThrowIfCanceled();
        }

        public virtual void VisitEndBlock(Block block)
        {
            ThrowIfCanceled();
        }

        public virtual void VisitError(RazorError err)
        {
            ThrowIfCanceled();
        }

        public virtual void OnComplete()
        {
            ThrowIfCanceled();
        }

        public virtual void ThrowIfCanceled()
        {
            if (CancelToken != null && CancelToken.Value.IsCancellationRequested)
            {
                throw new OperationCanceledException();
            }
        }
    }
}
