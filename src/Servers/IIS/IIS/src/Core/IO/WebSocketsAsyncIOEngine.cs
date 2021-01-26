// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Server.IIS.Core.IO
{
    internal partial class WebSocketsAsyncIOEngine: IAsyncIOEngine
    {
        private readonly IISHttpContext _context;

        private readonly NativeSafeHandle _handler;

        private bool _isInitialized;

        private AsyncInitializeOperation? _initializationFlush;

        private WebSocketWriteOperation? _cachedWebSocketWriteOperation;

        private WebSocketReadOperation? _cachedWebSocketReadOperation;

        private AsyncInitializeOperation? _cachedAsyncInitializeOperation;

        public WebSocketsAsyncIOEngine(IISHttpContext context, NativeSafeHandle handler)
        {
            _context = context;
            _handler = handler;
        }

        public ValueTask<int> ReadAsync(Memory<byte> memory)
        {
            lock (_context._contextLock)
            {
                ThrowIfNotInitialized();

                var read = GetReadOperation();
                read.Initialize(_handler, memory);
                read.Invoke();
                return new ValueTask<int>(read, 0);
            }
        }

        public ValueTask<int> WriteAsync(ReadOnlySequence<byte> data)
        {
            lock (_context._contextLock)
            {
                ThrowIfNotInitialized();

                var write = GetWriteOperation();
                write.Initialize(_handler, data);
                write.Invoke();
                return new ValueTask<int>(write, 0);
            }
        }

        public ValueTask FlushAsync(bool moreData)
        {
            lock (_context._contextLock)
            {
                if (_isInitialized)
                {
                    return new ValueTask(Task.CompletedTask);
                }

                NativeMethods.HttpEnableWebsockets(_handler);

                var init = GetInitializeOperation();
                init.Initialize(_handler);

                var continuation = init.Invoke();

                if (continuation != null)
                {
                    _isInitialized = true;
                }
                else
                {
                    _initializationFlush = init;
                }

                return new ValueTask(init, 0);
            }
        }

        public void NotifyCompletion(int hr, int bytes)
        {
            _isInitialized = true;

            var init = _initializationFlush;
            if (init == null)
            {
                throw new InvalidOperationException("Unexpected completion for WebSocket operation");
            }

            var continuation = init.Complete(hr, bytes);

            _initializationFlush = null;

            continuation.Invoke();
        }

        private void ThrowIfNotInitialized()
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Socket IO not initialized yet");
            }
        }

        public void Complete()
        {
            lock (_context._contextLock)
            {
                // Should only call CancelIO if the client hasn't disconnected
                if (!_context.ClientDisconnected)
                {
                    NativeMethods.HttpTryCancelIO(_handler);
                }
            }
        }

        private WebSocketReadOperation GetReadOperation() =>
            Interlocked.Exchange(ref _cachedWebSocketReadOperation, null) ??
            new WebSocketReadOperation(this);

        private WebSocketWriteOperation GetWriteOperation() =>
            Interlocked.Exchange(ref _cachedWebSocketWriteOperation, null) ??
            new WebSocketWriteOperation(this);

        private AsyncInitializeOperation GetInitializeOperation() =>
            Interlocked.Exchange(ref _cachedAsyncInitializeOperation, null) ??
            new AsyncInitializeOperation(this);

        private void ReturnOperation(AsyncInitializeOperation operation) =>
            Volatile.Write(ref _cachedAsyncInitializeOperation, operation);

        private void ReturnOperation(WebSocketWriteOperation operation) =>
            Volatile.Write(ref _cachedWebSocketWriteOperation, operation);

        private void ReturnOperation(WebSocketReadOperation operation) =>
            Volatile.Write(ref _cachedWebSocketReadOperation, operation);
    }
}
