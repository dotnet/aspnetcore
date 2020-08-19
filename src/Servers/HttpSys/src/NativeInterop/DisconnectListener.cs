// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using Microsoft.AspNetCore.HttpSys.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    internal class DisconnectListener
    {
        private readonly ConcurrentDictionary<ulong, ConnectionCancellation> _connectionCancellationTokens
            = new ConcurrentDictionary<ulong, ConnectionCancellation>();

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
                _logger.LogError(LoggerEventIds.DisconnectRegistrationError, exception, "Unable to register for disconnect notifications.");
                return CancellationToken.None;
            }
        }

        private CancellationToken GetOrCreateDisconnectToken(ulong connectionId)
        {
            // Read case is performance sensitive 
            ConnectionCancellation cancellation;
            if (!_connectionCancellationTokens.TryGetValue(connectionId, out cancellation))
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
            _logger.LogDebug(LoggerEventIds.RegisterDisconnectListener, "CreateDisconnectToken; Registering connection for disconnect for connection ID: {0}" , connectionId);

            // Create a nativeOverlapped callback so we can register for disconnect callback
            var cts = new CancellationTokenSource();
            var returnToken = cts.Token;

            SafeNativeOverlapped nativeOverlapped = null;
            var boundHandle = _requestQueue.BoundHandle;
            nativeOverlapped = new SafeNativeOverlapped(boundHandle, boundHandle.AllocateNativeOverlapped(
                (errorCode, numBytes, overlappedPtr) =>
                {
                    _logger.LogDebug(LoggerEventIds.DisconnectTriggered, "CreateDisconnectToken; http.sys disconnect callback fired for connection ID: {0}" , connectionId);

                    // Free the overlapped
                    nativeOverlapped.Dispose();

                    // Pull the token out of the list and Cancel it.
                    ConnectionCancellation token;
                    _connectionCancellationTokens.TryRemove(connectionId, out token);
                    try
                    {
                        cts.Cancel();
                    }
                    catch (AggregateException exception)
                    {
                        _logger.LogError(LoggerEventIds.DisconnectHandlerError, exception, "CreateDisconnectToken Callback");
                    }
                },
                null, null));

            uint statusCode;
            try
            {
                statusCode = HttpApi.HttpWaitForDisconnectEx(requestQueueHandle: _requestQueue.Handle,
                    connectionId: connectionId, reserved: 0, overlapped: nativeOverlapped);
            }
            catch (Win32Exception exception)
            {
                statusCode = (uint)exception.NativeErrorCode;
                _logger.LogError(LoggerEventIds.DisconnectRegistrationError, exception, "CreateDisconnectToken");
            }

            if (statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_IO_PENDING &&
                statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS)
            {
                // We got an unknown result, assume the connection has been closed.
                nativeOverlapped.Dispose();
                ConnectionCancellation ignored;
                _connectionCancellationTokens.TryRemove(connectionId, out ignored);
                _logger.LogDebug(LoggerEventIds.UnknownDisconnectError, new Win32Exception((int)statusCode), "HttpWaitForDisconnectEx");
                cts.Cancel();
            }

            if (statusCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS && HttpSysListener.SkipIOCPCallbackOnSuccess)
            {
                // IO operation completed synchronously - callback won't be called to signal completion
                nativeOverlapped.Dispose();
                ConnectionCancellation ignored;
                _connectionCancellationTokens.TryRemove(connectionId, out ignored);
                cts.Cancel();
            }

            return returnToken;
        }

        private class ConnectionCancellation
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
}
