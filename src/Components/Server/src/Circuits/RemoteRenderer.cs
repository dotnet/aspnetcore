// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using MessagePack;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Browser.Rendering
{
    internal class RemoteRenderer : HtmlRenderer
    {
        // The purpose of the timeout is just to ensure server resources are released at some
        // point if the client disconnects without sending back an ACK after a render
        private const int TimeoutMilliseconds = 60 * 1000;

        private readonly int _id;
        private readonly IJSRuntime _jsRuntime;
        private readonly CircuitClientProxy _client;
        private readonly RendererRegistry _rendererRegistry;
        private readonly ConcurrentDictionary<long, AutoCancelTaskCompletionSource<object>> _pendingRenders
            = new ConcurrentDictionary<long, AutoCancelTaskCompletionSource<object>>();
        private readonly ILogger _logger;
        private long _nextRenderId = 1;

        /// <summary>
        /// Notifies when a rendering exception occured.
        /// </summary>
        public event EventHandler<Exception> UnhandledException;

        /// <summary>
        /// Creates a new <see cref="RemoteRenderer"/>.
        /// </summary>
        public RemoteRenderer(
            IServiceProvider serviceProvider,
            RendererRegistry rendererRegistry,
            IJSRuntime jsRuntime,
            CircuitClientProxy client,
            IDispatcher dispatcher,
            HtmlEncoder encoder,
            ILogger logger)
            : base(serviceProvider, encoder.Encode, dispatcher)
        {
            _rendererRegistry = rendererRegistry;
            _jsRuntime = jsRuntime;
            _client = client;

            _id = _rendererRegistry.Add(this);
            _logger = logger;
        }

        internal ConcurrentQueue<byte[]> OfflineRenderBatches = new ConcurrentQueue<byte[]>();

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
                _id,
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
            base.Dispose(true);
            _rendererRegistry.TryRemove(_id);
        }

        /// <inheritdoc />
        protected override Task UpdateDisplayAsync(in RenderBatch batch)
        {
            // Note that we have to capture the data as a byte[] synchronously here, because
            // SignalR's SendAsync can wait an arbitrary duration before serializing the params.
            // The RenderBatch buffer will get reused by subsequent renders, so we need to
            // snapshot its contents now.
            // TODO: Consider using some kind of array pool instead of allocating a new
            //       buffer on every render.
            var batchBytes = MessagePackSerializer.Serialize(batch, RenderBatchFormatterResolver.Instance);

            if (!_client.Connected)
            {
                // Buffer the rendered batches while the client is disconnected. We'll send it down once the client reconnects.
                OfflineRenderBatches.Enqueue(batchBytes);
                return Task.CompletedTask;
            }

            Log.BeginUpdateDisplayAsync(_logger, _client.ConnectionId);
            return WriteBatchBytes(batchBytes);
        }

        public async Task ProcessBufferedRenderBatches()
        {
            // The server may discover that the client disconnected while we're attempting to write empty rendered batches.
            // Discontinue writing in this event.
            while (_client.Connected && OfflineRenderBatches.TryDequeue(out var renderBatch))
            {
                await WriteBatchBytes(renderBatch);
            }
        }

        private Task WriteBatchBytes(byte[] batchBytes)
        {
            var renderId = Interlocked.Increment(ref _nextRenderId);

            var pendingRenderInfo = new AutoCancelTaskCompletionSource<object>(TimeoutMilliseconds);
            _pendingRenders[renderId] = pendingRenderInfo;

            // Send the render batch to the client
            // If the "send" operation fails (synchronously or asynchronously), abort
            // the whole render with that exception
            try
            {
                _client.SendAsync("JS.RenderBatch", _id, renderId, batchBytes).ContinueWith(sendTask =>
                {
                    if (sendTask.IsFaulted)
                    {
                        pendingRenderInfo.TrySetException(sendTask.Exception);
                    }
                });
            }
            catch (Exception syncException)
            {
                pendingRenderInfo.TrySetException(syncException);
            }

            // When the render is completed (success, fail, or timeout), stop tracking it
            return pendingRenderInfo.Task.ContinueWith(task =>
            {
                _pendingRenders.TryRemove(renderId, out var ignored);
                if (task.IsFaulted)
                {
                    UnhandledException?.Invoke(this, task.Exception);
                }
            });
        }

        public void OnRenderCompleted(long renderId, string errorMessageOrNull)
        {
            if (_pendingRenders.TryGetValue(renderId, out var pendingRenderInfo))
            {
                if (errorMessageOrNull == null)
                {
                    pendingRenderInfo.TrySetResult(null);
                }
                else
                {
                    pendingRenderInfo.TrySetException(
                        new RemoteRendererException(errorMessageOrNull));
                }
            }
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
            private static readonly Action<ILogger, string, Exception> _beginUpdateDisplayAsync;
            private static readonly Action<ILogger, string, Exception> _bufferingRenderDisconnectedClient;

            private static class EventIds
            {
                public static readonly EventId UnhandledExceptionRenderingComponent = new EventId(100, "ExceptionRenderingComponent");
                public static readonly EventId BeginUpdateDisplayAsync = new EventId(101, "BeginUpdateDisplayAsync");
                public static readonly EventId SkipUpdateDisplayAsync = new EventId(102, "SkipUpdateDisplayAsync");
            }

            static Log()
            {
                _unhandledExceptionRenderingComponent = LoggerMessage.Define<string>(
                    LogLevel.Warning,
                    EventIds.UnhandledExceptionRenderingComponent,
                    "Unhandled exception rendering component: {Message}");

                _beginUpdateDisplayAsync = LoggerMessage.Define<string>(
                    LogLevel.Trace,
                    EventIds.BeginUpdateDisplayAsync,
                    "Begin remote rendering of components on client {ConnectionId}.");

                _bufferingRenderDisconnectedClient = LoggerMessage.Define<string>(
                    LogLevel.Trace,
                    EventIds.SkipUpdateDisplayAsync,
                    "Buffering remote render because the client on connection {ConnectionId} is disconnected.");
            }

            public static void UnhandledExceptionRenderingComponent(ILogger logger, Exception exception)
            {
                _unhandledExceptionRenderingComponent(
                    logger,
                    exception.Message,
                    exception);
            }

            public static void BeginUpdateDisplayAsync(ILogger logger, string connectionId)
            {
                _beginUpdateDisplayAsync(
                    logger,
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
        }
    }
}
