// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.AspNetCore.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNetCore.Razor.Parser.Internal
{
    public class CallbackVisitor : ParserVisitor
    {
        private Action<Span> _spanCallback;
        private Action<RazorError> _errorCallback;
        private Action<BlockType> _endBlockCallback;
        private Action<BlockType> _startBlockCallback;
        private Action _completeCallback;

        public CallbackVisitor(Action<Span> spanCallback)
            : this(spanCallback, _ =>
            {
            })
        {
        }

        public CallbackVisitor(Action<Span> spanCallback, Action<RazorError> errorCallback)
            : this(spanCallback, errorCallback, _ =>
            {
            }, _ =>
            {
            })
        {
        }

        public CallbackVisitor(Action<Span> spanCallback, Action<RazorError> errorCallback, Action<BlockType> startBlockCallback, Action<BlockType> endBlockCallback)
            : this(spanCallback, errorCallback, startBlockCallback, endBlockCallback, () =>
            {
            })
        {
        }

        public CallbackVisitor(Action<Span> spanCallback, Action<RazorError> errorCallback, Action<BlockType> startBlockCallback, Action<BlockType> endBlockCallback, Action completeCallback)
        {
            _spanCallback = spanCallback;
            _errorCallback = errorCallback;
            _startBlockCallback = startBlockCallback;
            _endBlockCallback = endBlockCallback;
            _completeCallback = completeCallback;
        }

        public SynchronizationContext SynchronizationContext { get; set; }

        public override void VisitStartBlock(Block block)
        {
            base.VisitStartBlock(block);
            RaiseCallback(SynchronizationContext, block.Type, _startBlockCallback);
        }

        public override void VisitSpan(Span span)
        {
            base.VisitSpan(span);
            RaiseCallback(SynchronizationContext, span, _spanCallback);
        }

        public override void VisitEndBlock(Block block)
        {
            base.VisitEndBlock(block);
            RaiseCallback(SynchronizationContext, block.Type, _endBlockCallback);
        }

        public override void VisitError(RazorError err)
        {
            base.VisitError(err);
            RaiseCallback(SynchronizationContext, err, _errorCallback);
        }

        public override void OnComplete()
        {
            base.OnComplete();
            RaiseCallback<object>(SynchronizationContext, null, _ => _completeCallback());
        }

        private static void RaiseCallback<T>(SynchronizationContext syncContext, T param, Action<T> callback)
        {
            if (callback != null)
            {
                if (syncContext != null)
                {
                    syncContext.Post(state => callback((T)state), param);
                }
                else
                {
                    callback(param);
                }
            }
        }
    }
}
