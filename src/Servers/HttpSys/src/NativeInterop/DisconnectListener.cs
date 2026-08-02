// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.ComponentModel;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.HttpSys;

internal sealed partial class DisconnectListener
{
    private readonly ConcurrentDictionary<ulong, ConnectionCancellation> _connectionCancellationTokens = new();

    private readonly RequestQueue _requestQueue;
    private readonly ILogger _logger;

    internal DisconnectListener(RequestQueue requestQueue, ILogger logger)
    {
        _requestQueue = requestQueue;
        _logger = logger;
    }

    internal CancellationToken GetTokenForConnection(ulong connectionId)
    {
        try
        {
            // Create exactly one CancellationToken per connection.
            return GetOrCreateDisconnectToken(connectionId);
        }
        catch (Win32Exception exception)
        {
            Log.DisconnectRegistrationError(_logger, exception);
            return CancellationToken.None;
        }
    }

    private CancellationToken GetOrCreateDisconnectToken(ulong connectionId)
    {
        // Read case is performance sensitive
        if (!_connectionCancellationTokens.TryGetValue(connectionId, out var cancellation))
        {
            cancellation = GetCreatedConnectionCancellation(connectionId);
        }
        return cancellation.GetCancellationToken(connectionId);
    }

    private ConnectionCancellation GetCreatedConnectionCancellation(ulong connectionId)
    {
        // Race condition on creation has no side effects
        var cancellation = new ConnectionCancellation(this);
        return _connectionCancellationTokens.GetOrAdd(connectionId, cancellation);
    }

    private unsafe CancellationToken CreateDisconnectToken(ulong connectionId)
    {
        Log.RegisterDisconnectListener(_logger, connectionId);

        // Create a nativeOverlapped callback so we can register for disconnect callback
        var cts = new CancellationTokenSource();
        var returnToken = cts.Token;
        var boundHandle = _requestQueue.BoundHandle;

        // Making sure we don't capture the execution context
        var nativeOverlapped = boundHandle.UnsafeAllocateNativeOverlapped((errorCode, numBytes, pOverlapped) =>
        {
            Log.DisconnectTriggered(_logger, connectionId);

            // Free the overlapped
            boundHandle.FreeNativeOverlapped(pOverlapped);

            // Pull the token out of the list and Cancel it.
            _connectionCancellationTokens.TryRemove(connectionId, out _);
            try
            {
                cts.Cancel();
            }
            catch (AggregateException exception)
            {
                Log.DisconnectHandlerError(_logger, exception);
            }
        },
        null,
        null);

        uint statusCode;
        try
        {
            statusCode = HttpApi.HttpWaitForDisconnectEx(requestQueueHandle: _requestQueue.Handle,
                connectionId: connectionId, reserved: 0, overlapped: nativeOverlapped);
        }
        catch (Win32Exception exception)
        {
            statusCode = (uint)exception.NativeErrorCode;
            Log.CreateDisconnectTokenError(_logger, exception);
        }

        if (statusCode != ErrorCodes.ERROR_IO_PENDING &&
            statusCode != ErrorCodes.ERROR_SUCCESS)
        {
            // We got an unknown result, assume the connection has been closed.
            boundHandle.FreeNativeOverlapped(nativeOverlapped);
            _connectionCancellationTokens.TryRemove(connectionId, out _);
            Log.UnknownDisconnectError(_logger, new Win32Exception((int)statusCode));
            cts.Cancel();
        }

        if (statusCode == ErrorCodes.ERROR_SUCCESS && HttpSysListener.SkipIOCPCallbackOnSuccess)
        {
            // IO operation completed synchronously - callback won't be called to signal completion
            boundHandle.FreeNativeOverlapped(nativeOverlapped);
            _connectionCancellationTokens.TryRemove(connectionId, out _);
            cts.Cancel();
        }

        return returnToken;
    }

    private sealed class ConnectionCancellation
    {
        private readonly DisconnectListener _parent;
        private volatile bool _initialized; // Must be volatile because initialization is synchronized
        private CancellationToken _cancellationToken;

        public ConnectionCancellation(DisconnectListener parent)
        {
            _parent = parent;
        }

        internal CancellationToken GetCancellationToken(ulong connectionId)
        {
            // Initialized case is performance sensitive
            if (_initialized)
            {
                return _cancellationToken;
            }
            return InitializeCancellationToken(connectionId);
        }

        private CancellationToken InitializeCancellationToken(ulong connectionId)
        {
            object syncObject = this;
#pragma warning disable 420 // Disable warning about volatile by reference since EnsureInitialized does volatile operations
            return LazyInitializer.EnsureInitialized(ref _cancellationToken, ref _initialized, ref syncObject, () => _parent.CreateDisconnectToken(connectionId));
#pragma warning restore 420
        }
    }
}
