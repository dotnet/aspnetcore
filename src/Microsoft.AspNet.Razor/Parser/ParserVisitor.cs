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
            foreach (SyntaxTreeNode node in block.Children)
            {
                node.Accept(this);
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
