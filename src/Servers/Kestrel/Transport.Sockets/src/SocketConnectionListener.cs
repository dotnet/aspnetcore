// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
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

        public EndPoint Endpoint { get; private set; }

        internal SocketConnectionListener(
            EndPoint endpoint,
            int ioQueueCount,
            ISocketsTrace trace,
            MemoryPool<byte> memoryPool)
        {
            Debug.Assert(endpoint != null);
            Debug.Assert(endpoint is IPEndPoint);
            Debug.Assert(trace != null);

            Endpoint = endpoint;
            _trace = trace;
            _memoryPool = memoryPool;

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

            var ip = (IPEndPoint)Endpoint;

            var listenSocket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            // Kestrel expects IPv6Any to bind to both IPv6 and IPv4
            if (ip.Address == IPAddress.IPv6Any)
            {
                listenSocket.DualMode = true;
            }

            try
            {
                listenSocket.Bind(ip);
            }
            catch (SocketException e) when (e.SocketErrorCode == SocketError.AddressAlreadyInUse)
            {
                throw new AddressInUseException(e.Message, e);
            }

            Endpoint = listenSocket.LocalEndPoint;

            listenSocket.Listen(512);

            _listenSocket = listenSocket;
        }

        public async ValueTask<ConnectionContext> AcceptAsync()
        {
            try
            {
                var acceptSocket = await _listenSocket.AcceptAsync();

                // REVIEW: This doesn't work anymore, these need to be on SocketOptions
                // acceptSocket.NoDelay = _endPointInformation.NoDelay;

                var connection = new SocketConnection(acceptSocket, _memoryPool, _schedulers[_schedulerIndex], _trace);

                connection.Start();

                _schedulerIndex = (_schedulerIndex + 1) % _numSchedulers;

                return connection;
            }
            catch (SocketException ex)
            {
                // REVIEW: Do we need more exception types?
                throw new ConnectionResetException("The connection was reset", ex);
            }
        }

        public ValueTask DisposeAsync()
        {
            _listenSocket?.Dispose();
            _listenSocket = null;
            _memoryPool.Dispose();
            // TODO: Wait for all connections to drain (fixed timeout?)
            return default;
        }

    }
}
