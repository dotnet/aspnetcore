// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Internal;
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

        internal ConcurrentQueue<UnacknowledgedRenderBatch> UnacknowledgedRenderBatches = new ConcurrentQueue<UnacknowledgedRenderBatch>();

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
            _rendererRegistry.TryRemove(Id);
            while (UnacknowledgedRenderBatches.TryDequeue(out var entry))
            {
                entry.CompletionSource.TrySetCanceled();
                entry.Data.Dispose();
            }
            base.Dispose(true);
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
            var arrayBuilder = new ArrayBuilder<byte>(2048);
            using var memoryStream = new ArrayBuilderMemoryStream(arrayBuilder);
            UnacknowledgedRenderBatch pendingRender;
            try
            {
                using (var renderBatchWriter = new RenderBatchWriter(memoryStream, false))
                {
                    renderBatchWriter.Write(in batch);
                }

                var renderId = Interlocked.Increment(ref _nextRenderId);

                var valueStopWatch = _logger.IsEnabled(LogLevel.Debug) ?
                    ValueStopwatch.StartNew() :
                    (ValueStopwatch?)null;

                pendingRender = new UnacknowledgedRenderBatch(
                    renderId,
                    arrayBuilder,
                    new TaskCompletionSource<object>(),
                    valueStopWatch);

                // Buffer the rendered batches no matter what. We'll send it down immediately when the client
                // is connected or right after the client reconnects.

                UnacknowledgedRenderBatches.Enqueue(pendingRender);
            }
            catch
            {
                // if we throw prior to queueing the write, dispose the builder.
                arrayBuilder.Dispose();
                throw;
            }

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
            return Task.WhenAll(UnacknowledgedRenderBatches.Select(b => WriteBatchBytesAsync(b)));
        }

        private async Task WriteBatchBytesAsync(UnacknowledgedRenderBatch pending)
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

                Log.BeginUpdateDisplayAsync(_logger, _client.ConnectionId, pending.BatchId, pending.Data.Count);
                var segment = new ArraySegment<byte>(pending.Data.Buffer, 0, pending.Data.Count);
                await _client.SendAsync("JS.RenderBatch", Id, pending.BatchId, segment);
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

            // When clients send acks we know for sure they received and applied the batch.
            // We send batches right away, and hold them in memory until we receive an ACK.
            // If one or more client ACKs get lost (e.g., with long polling, client->server delivery is not guaranteed)
            // we might receive an ack for a higher batch.
            // We confirm all previous batches at that point (because receiving an ack is guarantee
            // from the client that it has received and successfully applied all batches up to that point).

            // If receive an ack for a previously acknowledged batch, its an error, as the messages are
            // guranteed to be delivered in order, so a message for a render batch of 2 will never arrive
            // after a message for a render batch for 3.
            // If that were to be the case, it would just be enough to relax the checks here and simply skip
            // the message.

            // A batch might get lost when we send it to the client, because the client might disconnect before receiving and processing it.
            // In this case, once it reconnects the server will re-send any unacknowledged batches, some of which the
            // client might have received and even believe it did send back an acknowledgement for. The client handles
            // those by re-acknowledging.

            // Even though we're not on the renderer sync context here, it's safe to assume ordered execution of the following
            // line (i.e., matching the order in which we received batch completion messages) based on the fact that SignalR
            // synchronizes calls to hub methods. That is, it won't issue more than one call to this method from the same hub
            // at the same time on different threads.

            if (!UnacknowledgedRenderBatches.TryPeek(out var nextUnacknowledgedBatch) || incomingBatchId < nextUnacknowledgedBatch.BatchId)
            {
                Log.ReceivedDuplicateBatchAck(_logger, incomingBatchId);
            }
            else
            {
                var lastBatchId = nextUnacknowledgedBatch.BatchId;
                // Order is important here so that we don't prematurely dequeue the last nextUnacknowledgedBatch
                while (UnacknowledgedRenderBatches.TryPeek(out nextUnacknowledgedBatch) && nextUnacknowledgedBatch.BatchId <= incomingBatchId)
                {
                    lastBatchId = nextUnacknowledgedBatch.BatchId;
                    UnacknowledgedRenderBatches.TryDequeue(out _);
                    ProcessPendingBatch(errorMessageOrNull, nextUnacknowledgedBatch);
                }

                if (lastBatchId < incomingBatchId)
                {
                    HandleException(
                        new InvalidOperationException($"Received an acknowledgement for batch with id '{incomingBatchId}' when the last batch produced was '{lastBatchId}'."));
                }
            }
        }

        private void ProcessPendingBatch(string errorMessageOrNull, UnacknowledgedRenderBatch entry)
        {
            if (errorMessageOrNull == null)
            {
                Log.CompletingBatchWithoutError(_logger, entry.BatchId, entry.ValueStopwatch);
            }
            else
            {
                Log.CompletingBatchWithError(_logger, entry.BatchId, errorMessageOrNull, entry.ValueStopwatch);
            }

            entry.Data.Dispose();
            CompleteRender(entry.CompletionSource, errorMessageOrNull);
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

        internal readonly struct UnacknowledgedRenderBatch
        {
            public UnacknowledgedRenderBatch(long batchId, ArrayBuilder<byte> data, TaskCompletionSource<object> completionSource, ValueStopwatch? valueStopwatch)
            {
                BatchId = batchId;
                Data = data;
                CompletionSource = completionSource;
                ValueStopwatch = valueStopwatch;
            }

            public long BatchId { get; }
            public ArrayBuilder<byte> Data { get; }
            public TaskCompletionSource<object> CompletionSource { get; }
            public ValueStopwatch? ValueStopwatch { get; }
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
            private static readonly Action<ILogger, long, string, double, Exception> _completingBatchWithError;
            private static readonly Action<ILogger, long, double, Exception> _completingBatchWithoutError;
            private static readonly Action<ILogger, long, Exception> _receivedDuplicateBatchAcknowledgement;

            private static class EventIds
            {
                public static readonly EventId UnhandledExceptionRenderingComponent = new EventId(100, "ExceptionRenderingComponent");
                public static readonly EventId BeginUpdateDisplayAsync = new EventId(101, "BeginUpdateDisplayAsync");
                public static readonly EventId SkipUpdateDisplayAsync = new EventId(102, "SkipUpdateDisplayAsync");
                public static readonly EventId SendBatchDataFailed = new EventId(103, "SendBatchDataFailed");
                public static readonly EventId CompletingBatchWithError = new EventId(104, "CompletingBatchWithError");
                public static readonly EventId CompletingBatchWithoutError = new EventId(105, "CompletingBatchWithoutError");
                public static readonly EventId ReceivedDuplicateBatchAcknowledgement = new EventId(106, "ReceivedDuplicateBatchAcknowledgement");
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

                _completingBatchWithError = LoggerMessage.Define<long, string, double>(
                    LogLevel.Debug,
                    EventIds.CompletingBatchWithError,
                    "Completing batch {BatchId} with error: {ErrorMessage} in {ElapsedTime}ms.");

                _completingBatchWithoutError = LoggerMessage.Define<long, double>(
                    LogLevel.Debug,
                    EventIds.CompletingBatchWithoutError,
                    "Completing batch {BatchId} without error in {ElapsedTime}ms.");

                _receivedDuplicateBatchAcknowledgement = LoggerMessage.Define<long>(
                    LogLevel.Debug,
                    EventIds.ReceivedDuplicateBatchAcknowledgement,
                    "Received a duplicate ACK for batch id '{IncomingBatchId}'.");
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

            public static void CompletingBatchWithError(ILogger logger, long batchId, string errorMessage, ValueStopwatch? stopwatch)
            {
                Debug.Assert(stopwatch != null);
                var elapsedTime = stopwatch.Value.GetElapsedTime().TotalMilliseconds;

                _completingBatchWithError(
                    logger,
                    batchId,
                    errorMessage,
                    elapsedTime,
                    null);
            }

            public static void CompletingBatchWithoutError(ILogger logger, long batchId, ValueStopwatch? stopwatch)
            {
                Debug.Assert(stopwatch != null);
                var elapsedTime = stopwatch.Value.GetElapsedTime().TotalMilliseconds;

                _completingBatchWithoutError(
                    logger,
                    batchId,
                    elapsedTime,
                    null);
            }

            internal static void ReceivedDuplicateBatchAck(ILogger logger, long incomingBatchId)
            {
                _receivedDuplicateBatchAcknowledgement(logger, incomingBatchId, null);
            }
        }
    }
}
