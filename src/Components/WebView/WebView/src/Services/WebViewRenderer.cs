// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.WebView.Services
{
    internal class WebViewRenderer : Renderer
    {
        private readonly Queue<UnacknowledgedRenderBatch> _unacknowledgedRenderBatches = new();
        private readonly Dispatcher _dispatcher;
        private readonly IpcSender _ipcSender;
        private long nextRenderBatchId = 1;

        public WebViewRenderer(
            IServiceProvider serviceProvider,
            Dispatcher dispatcher,
            IpcSender ipcSender,
            ILoggerFactory loggerFactory,
            ElementReferenceContext elementReferenceContext) :
            base(serviceProvider, loggerFactory)
        {
            _dispatcher = dispatcher;
            _ipcSender = ipcSender;

            ElementReferenceContext = elementReferenceContext;
        }

        public override Dispatcher Dispatcher => _dispatcher;

        protected override void HandleException(Exception exception)
        {
            // Notify the JS code so it can show the in-app UI
            _ipcSender.NotifyUnhandledException(exception);

            // Also rethrow so the AppDomain's UnhandledException handler gets notified
            ExceptionDispatchInfo.Capture(exception).Throw();
        }

        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
        {
            var batchId = nextRenderBatchId++;
            var tcs = new TaskCompletionSource();
            _unacknowledgedRenderBatches.Enqueue(new UnacknowledgedRenderBatch
            {
                BatchId = batchId,
                CompletionSource = tcs,
            });

            _ipcSender.ApplyRenderBatch(batchId, renderBatch);
            return tcs.Task;
        }

        public int AddRootComponent(Type componentType, string domElementSelector)
        {
            var component = InstantiateComponent(componentType);
            var componentId = AssignRootComponentId(component);

            _ipcSender.AttachToDocument(componentId, domElementSelector);

            return componentId;
        }

        public new Task RenderRootComponentAsync(int componentId, ParameterView parameters)
           => base.RenderRootComponentAsync(componentId, parameters);

        public new void RemoveRootComponent(int componentId)
           => base.RemoveRootComponent(componentId);

        public void NotifyRenderCompleted(long batchId)
        {
            var nextUnacknowledgedBatch = _unacknowledgedRenderBatches.Dequeue();
            if (nextUnacknowledgedBatch.BatchId != batchId)
            {
                throw new InvalidOperationException($"Received unexpected acknowledgement for render batch {batchId} (next batch should be {nextUnacknowledgedBatch.BatchId})");
            }

            nextUnacknowledgedBatch.CompletionSource.SetResult();
        }

        record UnacknowledgedRenderBatch
        {
            public long BatchId { get; init; }
            public TaskCompletionSource CompletionSource { get; init; }
        }
    }
}
