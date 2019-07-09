// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Web.Rendering
{
    internal class RemoteRenderer : HtmlRenderer
    {
        private static readonly Task CanceledTask = Task.FromCanceled(new CancellationToken(canceled: true));

        private readonly IJSRuntime _jsRuntime;
        private readonly CircuitClientProxy _client;
        private readonly RendererRegistry _rendererRegistry;
        private readonly ILogger _logger;
        private long _nextRenderId = 1;
        private bool _disposing = false;

        /// <summary>
        /// Notifies when a rendering exception occured.
        /// </summary>
        public event EventHandler<Exception> UnhandledException;

        /// <summary>
        /// Creates a new <see cref="RemoteRenderer"/>.
        /// </summary>
        public RemoteRenderer(
            IServiceProvider serviceProvider,
            ILoggerFactory loggerFactory,
            RendererRegistry rendererRegistry,
            IJSRuntime jsRuntime,
            CircuitClientProxy client,
            HtmlEncoder encoder,
            ILogger logger)
            : base(serviceProvider, loggerFactory, encoder.Encode)
        {
            _rendererRegistry = rendererRegistry;
            _jsRuntime = jsRuntime;
            _client = client;

            Id = _rendererRegistry.Add(this);
            _logger = logger;
        }

        internal ConcurrentQueue<PendingRender> PendingRenderBatches = new ConcurrentQueue<PendingRender>();

        public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

        public int Id { get; }

        /// <summary>
        /// Associates the <see cref="IComponent"/> with the <see cref="RemoteRenderer"/>,
        /// causing it to be displayed in the specified DOM element.
        /// </summary>
        /// <param name="componentType">The type of the component.</param>
        /// <param name="domElementSelector">A CSS selector that uniquely identifies a DOM element.</param>
        public Task AddComponentAsync(Type componentType, string domElementSelector)
        {
            var component = InstantiateComponent(componentType);
            var componentId = AssignRootComponentId(component);

            var attachComponentTask = _jsRuntime.InvokeAsync<object>(
                "Blazor._internal.attachRootComponentToElement",
                Id,
                domElementSelector,
                componentId);
            CaptureAsyncExceptions(attachComponentTask);

            return RenderRootComponentAsync(componentId);
        }

        /// <inheritdoc />
        protected override void HandleException(Exception exception)
        {
            if (exception is AggregateException aggregateException)
            {
                foreach (var innerException in aggregateException.Flatten().InnerExceptions)
                {
                    Log.UnhandledExceptionRenderingComponent(_logger, innerException);
                }
            }
            else
            {
                Log.UnhandledExceptionRenderingComponent(_logger, exception);
            }

            UnhandledException?.Invoke(this, exception);
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            _disposing = true;
            base.Dispose(true);
            while (PendingRenderBatches.TryDequeue(out var entry))
            {
                entry.CompletionSource.TrySetCanceled();
            }
            _rendererRegistry.TryRemove(Id);
        }

        /// <inheritdoc />
        protected override Task UpdateDisplayAsync(in RenderBatch batch)
        {
            if (_disposing)
            {
                // We are being disposed, so do no work.
                return CanceledTask;
            }

            // Note that we have to capture the data as a byte[] synchronously here, because
            // SignalR's SendAsync can wait an arbitrary duration before serializing the params.
            // The RenderBatch buffer will get reused by subsequent renders, so we need to
            // snapshot its contents now.
            // TODO: Consider using some kind of array pool instead of allocating a new
            //       buffer on every render.
            byte[] batchBytes;
            using (var memoryStream = new MemoryStream())
            {
                using (var renderBatchWriter = new RenderBatchWriter(memoryStream, false))
                {
                    renderBatchWriter.Write(in batch);
                }

                batchBytes = memoryStream.ToArray();
            }

            var renderId = Interlocked.Increment(ref _nextRenderId);

            var pendingRender = new PendingRender(
                renderId,
                batchBytes,
                new TaskCompletionSource<object>());

            // Buffer the rendered batches no matter what. We'll send it down immediately when the client
            // is connected or right after the client reconnects.

            PendingRenderBatches.Enqueue(pendingRender);

            // Fire and forget the initial send for this batch (if connected). Otherwise it will be sent
            // as soon as the client reconnects.
            var _ = WriteBatchBytesAsync(pendingRender);

            return pendingRender.CompletionSource.Task;
        }

        public Task ProcessBufferedRenderBatches()
        {
            // All the batches are sent in order based on the fact that SignalR
            // provides ordering for the underlying messages and that the batches
            // are always in order.
            return Task.WhenAll(PendingRenderBatches.Select(b => WriteBatchBytesAsync(b)));
        }

        private async Task WriteBatchBytesAsync(PendingRender pending)
        {
            // Send the render batch to the client
            // If the "send" operation fails (synchronously or asynchronously) or the client
            // gets disconected simply give up. This likely means that
            // the circuit went offline while sending the data, so simply wait until the
            // client reconnects back or the circuit gets evicted because it stayed
            // disconnected for too long.

            try
            {
                if (!_client.Connected)
                {
                    // If we detect that the client is offline. Simply stop trying to send the payload.
                    // When the client reconnects we'll resend it.
                    return;
                }

                Log.BeginUpdateDisplayAsync(_logger, _client.ConnectionId, pending.BatchId, pending.Data.Length);
                await _client.SendAsync("JS.RenderBatch", Id, pending.BatchId, pending.Data);
            }
            catch (Exception e)
            {
                Log.SendBatchDataFailed(_logger, e);
            }

            // We don't have to remove the entry from the list of pending batches if we fail to send it or the client fails to
            // acknowledge that it received it. We simply keep it in the queue until we receive another ack from the client for
            // a later batch (clientBatchId > thisBatchId) or the circuit becomes disconnected and we ultimately get evicted and
            // disposed.
        }

        public void OnRenderCompleted(long incomingBatchId, string errorMessageOrNull)
        {
            if (_disposing)
            {
                // Disposing so don't do work.
                return;
            }

            // Even though we're not on the renderer sync context here, it's safe to assume ordered execution of the following
            // line (i.e., matching the order in which we received batch completion messages) based on the fact that SignalR
            // synchronizes calls to hub methods. That is, it won't issue more than one call to this method from the same hub
            // at the same time on different threads.
            if (!PendingRenderBatches.TryDequeue(out var entry) || entry.BatchId != incomingBatchId)
            {
                HandleException(
                    new InvalidOperationException($"Received a notification for a rendered batch when not expecting it. Batch id '{incomingBatchId}'."));
            }
            else
            {
                if (errorMessageOrNull == null)
                {
                    Log.CompletingBatchWithoutError(_logger, entry.BatchId);
                }
                else
                {
                    Log.CompletingBatchWithError(_logger, entry.BatchId, errorMessageOrNull);
                }

                CompleteRender(entry.CompletionSource, errorMessageOrNull);
            }
        }

        private void CompleteRender(TaskCompletionSource<object> pendingRenderInfo, string errorMessageOrNull)
        {
            if (errorMessageOrNull == null)
            {
                pendingRenderInfo.TrySetResult(null);
            }
            else
            {
                pendingRenderInfo.TrySetException(new RemoteRendererException(errorMessageOrNull));
            }
        }

        internal readonly struct PendingRender
        {
            public PendingRender(long batchId, byte[] data, TaskCompletionSource<object> completionSource)
            {
                BatchId = batchId;
                Data = data;
                CompletionSource = completionSource;
            }

            public long BatchId { get; }
            public byte[] Data { get; }
            public TaskCompletionSource<object> CompletionSource { get; }
        }

        private void CaptureAsyncExceptions(Task task)
        {
            task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    UnhandledException?.Invoke(this, t.Exception);
                }
            });
        }

        private static class Log
        {
            private static readonly Action<ILogger, string, Exception> _unhandledExceptionRenderingComponent;
            private static readonly Action<ILogger, long, int, string, Exception> _beginUpdateDisplayAsync;
            private static readonly Action<ILogger, string, Exception> _bufferingRenderDisconnectedClient;
            private static readonly Action<ILogger, string, Exception> _sendBatchDataFailed;
            private static readonly Action<ILogger, long, string, Exception> _completingBatchWithError;
            private static readonly Action<ILogger, long, Exception> _completingBatchWithoutError;

            private static class EventIds
            {
                public static readonly EventId UnhandledExceptionRenderingComponent = new EventId(100, "ExceptionRenderingComponent");
                public static readonly EventId BeginUpdateDisplayAsync = new EventId(101, "BeginUpdateDisplayAsync");
                public static readonly EventId SkipUpdateDisplayAsync = new EventId(102, "SkipUpdateDisplayAsync");
                public static readonly EventId SendBatchDataFailed = new EventId(103, "SendBatchDataFailed");
                public static readonly EventId CompletingBatchWithError = new EventId(104, "CompletingBatchWithError");
                public static readonly EventId CompletingBatchWithoutError = new EventId(105, "CompletingBatchWithoutError");
            }

            static Log()
            {
                _unhandledExceptionRenderingComponent = LoggerMessage.Define<string>(
                    LogLevel.Warning,
                    EventIds.UnhandledExceptionRenderingComponent,
                    "Unhandled exception rendering component: {Message}");

                _beginUpdateDisplayAsync = LoggerMessage.Define<long, int, string>(
                    LogLevel.Debug,
                    EventIds.BeginUpdateDisplayAsync,
                    "Sending render batch {BatchId} of size {DataLength} bytes to client {ConnectionId}.");

                _bufferingRenderDisconnectedClient = LoggerMessage.Define<string>(
                    LogLevel.Debug,
                    EventIds.SkipUpdateDisplayAsync,
                    "Buffering remote render because the client on connection {ConnectionId} is disconnected.");

                _sendBatchDataFailed = LoggerMessage.Define<string>(
                    LogLevel.Information,
                    EventIds.SendBatchDataFailed,
                    "Sending data for batch failed: {Message}");

                _completingBatchWithError = LoggerMessage.Define<long, string>(
                    LogLevel.Debug,
                    EventIds.CompletingBatchWithError,
                    "Completing batch {BatchId} with error: {ErrorMessage}");

                _completingBatchWithoutError = LoggerMessage.Define<long>(
                    LogLevel.Debug,
                    EventIds.CompletingBatchWithoutError,
                    "Completing batch {BatchId} without error");
            }

            public static void SendBatchDataFailed(ILogger logger, Exception exception)
            {
                _sendBatchDataFailed(logger, exception.Message, exception);
            }

            public static void UnhandledExceptionRenderingComponent(ILogger logger, Exception exception)
            {
                _unhandledExceptionRenderingComponent(
                    logger,
                    exception.Message,
                    exception);
            }

            public static void BeginUpdateDisplayAsync(ILogger logger, string connectionId, long batchId, int dataLength)
            {
                _beginUpdateDisplayAsync(
                    logger,
                    batchId,
                    dataLength,
                    connectionId,
                    null);
            }

            public static void BufferingRenderDisconnectedClient(ILogger logger, string connectionId)
            {
                _bufferingRenderDisconnectedClient(
                    logger,
                    connectionId,
                    null);
            }

            public static void CompletingBatchWithError(ILogger logger, long batchId, string errorMessage)
            {
                _completingBatchWithError(
                    logger,
                    batchId,
                    errorMessage,
                    null);
            }

            public static void CompletingBatchWithoutError(ILogger logger, long batchId)
            {
                _completingBatchWithoutError(
                    logger,
                    batchId,
                    null);
            }
        }
    }
}
