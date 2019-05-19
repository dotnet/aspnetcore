// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets
{
    internal sealed class SocketConnectionListener : IConnectionListener
    {
        private readonly MemoryPool<byte> _memoryPool;
        private readonly int _numSchedulers;
        private readonly PipeScheduler[] _schedulers;
        private readonly ISocketsTrace _trace;
        private Socket _listenSocket;
        private int _schedulerIndex;
        private readonly SocketTransportOptions _options;
        private TaskCompletionSource<object> _connectionsDrainedTcs;
        private int _pendingConnections;
        private int _pendingAccepts;
        private readonly ConcurrentDictionary<int, TrackedSocketConnection> _connections = new ConcurrentDictionary<int, TrackedSocketConnection>();

        public EndPoint EndPoint { get; private set; }

        internal SocketConnectionListener(
            EndPoint endpoint,
            SocketTransportOptions options,
            ISocketsTrace trace)
        {
            Debug.Assert(endpoint != null);
            Debug.Assert(endpoint is IPEndPoint);
            Debug.Assert(trace != null);

            EndPoint = endpoint;
            _trace = trace;
            _options = options;
            _memoryPool = _options.MemoryPoolFactory();
            var ioQueueCount = options.IOQueueCount;

            if (ioQueueCount > 0)
            {
                _numSchedulers = ioQueueCount;
                _schedulers = new IOQueue[_numSchedulers];

                for (var i = 0; i < _numSchedulers; i++)
                {
                    _schedulers[i] = new IOQueue();
                }
            }
            else
            {
                var directScheduler = new PipeScheduler[] { PipeScheduler.ThreadPool };
                _numSchedulers = directScheduler.Length;
                _schedulers = directScheduler;
            }
        }

        internal void Bind()
        {
            if (_listenSocket != null)
            {
                throw new InvalidOperationException(SocketsStrings.TransportAlreadyBound);
            }

            var listenSocket = new Socket(EndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            // Kestrel expects IPv6Any to bind to both IPv6 and IPv4
            if (EndPoint is IPEndPoint ip && ip.Address == IPAddress.IPv6Any)
            {
                listenSocket.DualMode = true;
            }

            try
            {
                listenSocket.Bind(EndPoint);
            }
            catch (SocketException e) when (e.SocketErrorCode == SocketError.AddressAlreadyInUse)
            {
                throw new AddressInUseException(e.Message, e);
            }

            EndPoint = listenSocket.LocalEndPoint;

            listenSocket.Listen(512);

            _listenSocket = listenSocket;
        }

        public async ValueTask<ConnectionContext> AcceptAsync()
        {
            Interlocked.Increment(ref _pendingAccepts);

            try
            {
                var acceptSocket = await _listenSocket.AcceptAsync();
                acceptSocket.NoDelay = _options.NoDelay;

                int id = Interlocked.Increment(ref _pendingConnections);

                var connection = new TrackedSocketConnection(this, id, acceptSocket, _memoryPool, _schedulers[_schedulerIndex], _trace);
                _connections.TryAdd(id, connection);

                connection.Start();

                _schedulerIndex = (_schedulerIndex + 1) % _numSchedulers;

                return connection;
            }
            catch (SocketException)
            {
                return null;
            }
            finally
            {
                Interlocked.Decrement(ref _pendingAccepts);
            }
        }

        public async ValueTask StopAsync(CancellationToken cancellationToken)
        {
            var listenSocket = Interlocked.Exchange(ref _listenSocket, null);

            if (listenSocket != null)
            {
                // Unbind the listen socket, so no new connections can come in
                listenSocket.Dispose();

                // Wait for all pending accepts to drain
                var spin = new SpinWait();
                while (_pendingAccepts > 0)
                {
                    spin.SpinOnce();
                }

                // No more pending accepts by the time we get here, if there any any pending connections, we create a new TCS and wait for them to
                // drain.
                if (_pendingConnections > 0)
                {
                    _connectionsDrainedTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

                    // Try to do graceful close
                    foreach (var pair in _connections)
                    {
                        pair.Value.RequestClose();
                    }

                    if (!_connectionsDrainedTcs.Task.IsCompletedSuccessfully)
                    {
                        // Wait for connections to drain or for the token to fire
                        var task = CancellationTokenAsTask(cancellationToken);
                        var result = await Task.WhenAny(_connectionsDrainedTcs.Task, task);

                        if (result != _connectionsDrainedTcs.Task)
                        {
                            // If the connections don't shutdown then we need to abort them
                            foreach (var pair in _connections)
                            {
                                pair.Value.Abort();
                            }
                        }
                    }

                    // REVIEW: Should we try to wait again?
                    // await _connectionsDrainedTcs.Task;
                }
            }

            // Dispose the memory pool
            _memoryPool.Dispose();
        }

        private void OnConnectionDisposed(int id)
        {
            // Disconnect called, wait for _gracefulShutdownTcs to be assigned
            if (_listenSocket == null && _connectionsDrainedTcs == null)
            {
                var spin = new SpinWait();
                while (_connectionsDrainedTcs == null)
                {
                    spin.SpinOnce();
                }
            }

            // If a call to dispose is currently running then we need to wait until the _gracefulShutdownTcs
            // has been assigned
            var connections = Interlocked.Decrement(ref _pendingConnections);

            _connections.TryRemove(id, out _);

            if (_connectionsDrainedTcs != null && connections == 0)
            {
                _connectionsDrainedTcs.TrySetResult(null);
            }
        }

        private static Task CancellationTokenAsTask(CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                return Task.CompletedTask;
            }

            // Transports already dispatch prior to tripping ConnectionClosed
            // since application code can register to this token.
            var tcs = new TaskCompletionSource<object>();
            token.Register(state => ((TaskCompletionSource<object>)state).SetResult(null), tcs);
            return tcs.Task;
        }

        public ValueTask DisposeAsync()
        {
            return StopAsync(new CancellationTokenSource(5000).Token);
        }

        private class TrackedSocketConnection : SocketConnection
        {
            private readonly SocketConnectionListener _listener;
            private readonly int _id;

            internal TrackedSocketConnection(SocketConnectionListener listener, int id, Socket socket, MemoryPool<byte> memoryPool, PipeScheduler scheduler, ISocketsTrace trace) : base(socket, memoryPool, scheduler, trace)
            {
                _listener = listener;
                _id = id;
            }

            public override async ValueTask DisposeAsync()
            {
                await base.DisposeAsync();

                _listener.OnConnectionDisposed(_id);
            }
        }
    }
}
