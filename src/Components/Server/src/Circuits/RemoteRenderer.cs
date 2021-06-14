// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Server.Circuits
{
    internal class RemoteRenderer : Microsoft.AspNetCore.Components.RenderTree.Renderer
    {
        private static readonly Task CanceledTask = Task.FromCanceled(new CancellationToken(canceled: true));

        private readonly CircuitClientProxy _client;
        private readonly CircuitOptions _options;
        private readonly ILogger _logger;
        internal readonly ConcurrentQueue<UnacknowledgedRenderBatch> _unacknowledgedRenderBatches = new ConcurrentQueue<UnacknowledgedRenderBatch>();
        private long _nextRenderId = 1;
        private bool _disposing;

        /// <summary>
        /// Notifies when a rendering exception occurred.
        /// </summary>
        public event EventHandler<Exception>? UnhandledException;

        /// <summary>
        /// Creates a new <see cref="RemoteRenderer"/>.
        /// </summary>
        public RemoteRenderer(
            IServiceProvider serviceProvider,
            ILoggerFactory loggerFactory,
            CircuitOptions options,
            CircuitClientProxy client,
            ILogger logger,
            ElementReferenceContext? elementReferenceContext)
            : base(serviceProvider, loggerFactory)
        {
            _client = client;
            _options = options;
            _logger = logger;

            ElementReferenceContext = elementReferenceContext;
        }

        public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

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

            var attachComponentTask = _client.SendAsync("JS.AttachComponent", componentId, domElementSelector);
            _ = CaptureAsyncExceptions(attachComponentTask);

            return RenderRootComponentAsync(componentId);
        }

        /// <summary>
        /// Associates the <see cref="IComponent"/> with the <see cref="RemoteRenderer"/>,
        /// causing it to be displayed in the specified DOM element.
        /// </summary>
        /// <param name="componentType">The type of the component.</param>
        /// <param name="parameters">The parameters for the component.</param>
        /// <param name="domElementSelector">A CSS selector that uniquely identifies a DOM element.</param>
        public Task AddComponentAsync(Type componentType, ParameterView parameters, string domElementSelector)
        {
            var component = InstantiateComponent(componentType);
            var componentId = AssignRootComponentId(component);

            var attachComponentTask = _client.SendAsync("JS.AttachComponent", componentId, domElementSelector);
            _ = CaptureAsyncExceptions(attachComponentTask);

            return RenderRootComponentAsync(componentId, parameters);
        }

        protected override void ProcessPendingRender()
        {
            if (_unacknowledgedRenderBatches.Count >= _options.MaxBufferedUnacknowledgedRenderBatches)
            {
                // If we got here it means we are at max capacity, so we don't want to actually process the queue,
                // as we have a client that is not acknowledging render batches fast enough (something we consider needs
                // to be fast).
                // The result is something as follows:
                // Lets imagine an extreme case where the server produces a new batch every millisecond.
                // Lets say the client is able to ACK a batch every 100 milliseconds.
                // When the app starts the client might see the sequence 0->(MaxUnacknowledgedRenderBatches-1) and then
                // after 100 milliseconds it sees it jump to 1xx, then to 2xx where xx is something between {0..99} the
                // reason for this is that the server slows down rendering new batches to as fast as the client can consume
                // them.
                // Similarly, if a client were to send events at a faster pace than the server can consume them, the server
                // would still process the events, but would not produce new renders until it gets an ack that frees up space
                // for a new render.
                // We should never see UnacknowledgedRenderBatches.Count > _options.MaxBufferedUnacknowledgedRenderBatches

                // But if we do, it's safer to simply disable the rendering in that case too instead of allowing batches to
                Log.FullUnacknowledgedRenderBatchesQueue(_logger);

                return;
            }

            base.ProcessPendingRender();
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
            while (_unacknowledgedRenderBatches.TryDequeue(out var entry))
            {
                entry.CompletionSource.TrySetCanceled();
                entry.Data.Dispose();
            }
            base.Dispose(true);
        }

        /// <inheritdoc />
        protected override Task UpdateDisplayAsync(in Microsoft.AspNetCore.Components.RenderTree.RenderBatch batch)
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

                pendingRender = new UnacknowledgedRenderBatch(
                    renderId,
                    arrayBuilder,
                    new TaskCompletionSource(),
                    ValueStopwatch.StartNew());

                // Buffer the rendered batches no matter what. We'll send it down immediately when the client
                // is connected or right after the client reconnects.

                _unacknowledgedRenderBatches.Enqueue(pendingRender);
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
            return Task.WhenAll(_unacknowledgedRenderBatches.Select(b => WriteBatchBytesAsync(b)));
        }

        private async Task WriteBatchBytesAsync(UnacknowledgedRenderBatch pending)
        {
            // Send the render batch to the client
            // If the "send" operation fails (synchronously or asynchronously) or the client
            // gets disconnected simply give up. This likely means that
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
                await _client.SendAsync("JS.RenderBatch", pending.BatchId, segment);
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

        public Task OnRenderCompletedAsync(long incomingBatchId, string? errorMessageOrNull)
        {
            if (_disposing)
            {
                // Disposing so don't do work.
                return Task.CompletedTask;
            }

            // When clients send acks we know for sure they received and applied the batch.
            // We send batches right away, and hold them in memory until we receive an ACK.
            // If one or more client ACKs get lost (e.g., with long polling, client->server delivery is not guaranteed)
            // we might receive an ack for a higher batch.
            // We confirm all previous batches at that point (because receiving an ack is guarantee
            // from the client that it has received and successfully applied all batches up to that point).

            // If receive an ack for a previously acknowledged batch, its an error, as the messages are
            // guaranteed to be delivered in order, so a message for a render batch of 2 will never arrive
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

            if (!_unacknowledgedRenderBatches.TryPeek(out var nextUnacknowledgedBatch) || incomingBatchId < nextUnacknowledgedBatch.BatchId)
            {
                Log.ReceivedDuplicateBatchAck(_logger, incomingBatchId);
                return Task.CompletedTask;
            }
            else
            {
                var lastBatchId = nextUnacknowledgedBatch.BatchId;
                // Order is important here so that we don't prematurely dequeue the last nextUnacknowledgedBatch
                while (_unacknowledgedRenderBatches.TryPeek(out nextUnacknowledgedBatch) && nextUnacknowledgedBatch.BatchId <= incomingBatchId)
                {
                    lastBatchId = nextUnacknowledgedBatch.BatchId;
                    // At this point the queue is definitely not full, we have at least emptied one slot, so we allow a further
                    // full queue log entry the next time it fills up.
                    _unacknowledgedRenderBatches.TryDequeue(out _);
                    ProcessPendingBatch(errorMessageOrNull, nextUnacknowledgedBatch);
                }

                if (lastBatchId < incomingBatchId)
                {
                    // This exception is due to a bad client input, so we mark it as such to prevent logging it as a warning and
                    // flooding the logs with warnings.
                    throw new InvalidOperationException($"Received an acknowledgement for batch with id '{incomingBatchId}' when the last batch produced was '{lastBatchId}'.");
                }

                // Normally we will not have pending renders, but it might happen that we reached the limit of
                // available buffered renders and new renders got queued.
                // Invoke ProcessBufferedRenderRequests so that we might produce any additional batch that is
                // missing.

                // We return the task in here, but the caller doesn't await it.
                return Dispatcher.InvokeAsync(() =>
                {
                    // Now we're on the sync context, check again whether we got disposed since this
                    // work item was queued. If so there's nothing to do.
                    if (!_disposing)
                    {
                        ProcessPendingRender();
                    }
                });
            }
        }

        private void ProcessPendingBatch(string? errorMessageOrNull, UnacknowledgedRenderBatch entry)
        {
            var elapsedTime = entry.ValueStopwatch.GetElapsedTime();
            if (errorMessageOrNull == null)
            {
                Log.CompletingBatchWithoutError(_logger, entry.BatchId, elapsedTime);
            }
            else
            {
                Log.CompletingBatchWithError(_logger, entry.BatchId, errorMessageOrNull, elapsedTime);
            }

            entry.Data.Dispose();
            CompleteRender(entry.CompletionSource, errorMessageOrNull);
        }

        private static void CompleteRender(TaskCompletionSource pendingRenderInfo, string? errorMessageOrNull)
        {
            if (errorMessageOrNull == null)
            {
                pendingRenderInfo.TrySetResult();
            }
            else
            {
                pendingRenderInfo.TrySetException(new InvalidOperationException(errorMessageOrNull));
            }
        }

        internal readonly struct UnacknowledgedRenderBatch
        {
            public UnacknowledgedRenderBatch(long batchId, ArrayBuilder<byte> data, TaskCompletionSource completionSource, ValueStopwatch valueStopwatch)
            {
                BatchId = batchId;
                Data = data;
                CompletionSource = completionSource;
                ValueStopwatch = valueStopwatch;
            }

            public long BatchId { get; }
            public ArrayBuilder<byte> Data { get; }
            public TaskCompletionSource CompletionSource { get; }
            public ValueStopwatch ValueStopwatch { get; }
        }

        private async Task CaptureAsyncExceptions(Task task)
        {
            try
            {
                await task;
            }
            catch (Exception exception)
            {
                UnhandledException?.Invoke(this, exception);
            }
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
            private static readonly Action<ILogger, Exception> _fullUnacknowledgedRenderBatchesQueue;

            private static class EventIds
            {
                public static readonly EventId UnhandledExceptionRenderingComponent = new EventId(100, "ExceptionRenderingComponent");
                public static readonly EventId BeginUpdateDisplayAsync = new EventId(101, "BeginUpdateDisplayAsync");
                public static readonly EventId SkipUpdateDisplayAsync = new EventId(102, "SkipUpdateDisplayAsync");
                public static readonly EventId SendBatchDataFailed = new EventId(103, "SendBatchDataFailed");
                public static readonly EventId CompletingBatchWithError = new EventId(104, "CompletingBatchWithError");
                public static readonly EventId CompletingBatchWithoutError = new EventId(105, "CompletingBatchWithoutError");
                public static readonly EventId ReceivedDuplicateBatchAcknowledgement = new EventId(106, "ReceivedDuplicateBatchAcknowledgement");
                public static readonly EventId FullUnacknowledgedRenderBatchesQueue = new EventId(107, "FullUnacknowledgedRenderBatchesQueue");
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
                    "Completing batch {BatchId} with error: {ErrorMessage} in {ElapsedMilliseconds}ms.");

                _completingBatchWithoutError = LoggerMessage.Define<long, double>(
                    LogLevel.Debug,
                    EventIds.CompletingBatchWithoutError,
                    "Completing batch {BatchId} without error in {ElapsedMilliseconds}ms.");

                _receivedDuplicateBatchAcknowledgement = LoggerMessage.Define<long>(
                    LogLevel.Debug,
                    EventIds.ReceivedDuplicateBatchAcknowledgement,
                    "Received a duplicate ACK for batch id '{IncomingBatchId}'.");

                _fullUnacknowledgedRenderBatchesQueue = LoggerMessage.Define(
                    LogLevel.Debug,
                    EventIds.FullUnacknowledgedRenderBatchesQueue,
                    "The queue of unacknowledged render batches is full.");
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

            public static void CompletingBatchWithError(ILogger logger, long batchId, string errorMessage, TimeSpan elapsedTime)
            {
                _completingBatchWithError(
                    logger,
                    batchId,
                    errorMessage,
                    elapsedTime.TotalMilliseconds,
                    null);
            }

            public static void CompletingBatchWithoutError(ILogger logger, long batchId, TimeSpan elapsedTime)
            {
                _completingBatchWithoutError(
                    logger,
                    batchId,
                    elapsedTime.TotalMilliseconds,
                    null);
            }

            public static void ReceivedDuplicateBatchAck(ILogger logger, long incomingBatchId)
            {
                _receivedDuplicateBatchAcknowledgement(logger, incomingBatchId, null);
            }

            public static void FullUnacknowledgedRenderBatchesQueue(ILogger logger)
            {
                _fullUnacknowledgedRenderBatchesQueue(logger, null);
            }
        }
    }

    internal readonly struct PendingRender
    {
        public PendingRender(int componentId, RenderFragment renderFragment)
        {
            ComponentId = componentId;
            RenderFragment = renderFragment;
        }

        public int ComponentId { get; }
        public RenderFragment RenderFragment { get; }
    }
}
