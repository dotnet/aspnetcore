// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Microsoft.Net.Http.Server
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
                LogHelper.LogException(_logger, "GetConnectionToken", exception);
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
            LogHelper.LogDebug(_logger, "CreateDisconnectToken", "Registering connection for disconnect for connection ID: " + connectionId);

            // Create a nativeOverlapped callback so we can register for disconnect callback
            var cts = new CancellationTokenSource();

            SafeNativeOverlapped nativeOverlapped = null;
            var boundHandle = _requestQueue.BoundHandle;
            nativeOverlapped = new SafeNativeOverlapped(boundHandle, boundHandle.AllocateNativeOverlapped(
                (errorCode, numBytes, overlappedPtr) =>
                {
                    LogHelper.LogDebug(_logger, "CreateDisconnectToken", "http.sys disconnect callback fired for connection ID: " + connectionId);
                    
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
                        LogHelper.LogException(_logger, "CreateDisconnectToken Callback", exception);
                    }

                    cts.Dispose();
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
                LogHelper.LogException(_logger, "CreateDisconnectToken", exception);
            }

            if (statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_IO_PENDING &&
                statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS)
            {
                // We got an unknown result so return a None
                // TODO: return a canceled token?
                return CancellationToken.None;
            }

            if (statusCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS && WebListener.SkipIOCPCallbackOnSuccess)
            {
                // IO operation completed synchronously - callback won't be called to signal completion.
                // TODO: return a canceled token?
                return CancellationToken.None;
            }

            return cts.Token;
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
