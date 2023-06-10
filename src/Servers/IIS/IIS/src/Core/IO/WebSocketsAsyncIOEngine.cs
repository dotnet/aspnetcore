// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Server.IIS.Core.IO;

internal sealed partial class WebSocketsAsyncIOEngine : IAsyncIOEngine
{
    private readonly IISHttpContext _context;

    private readonly NativeSafeHandle _handler;

    private bool _isInitialized;

    private AsyncInitializeOperation? _initializationFlush;

    private WebSocketWriteOperation? _webSocketWriteOperation;

    private WebSocketReadOperation? _webSocketReadOperation;

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

            var read = _webSocketReadOperation ??= new WebSocketReadOperation(this);

            Debug.Assert(!read.InUse());

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

            var write = _webSocketWriteOperation ??= new WebSocketWriteOperation(this);

            Debug.Assert(!write.InUse());

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

            var init = new AsyncInitializeOperation(this);
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

    public void Dispose()
    {
        _webSocketWriteOperation?.Dispose();
        _webSocketReadOperation?.Dispose();
    }
}
