// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets
{
    internal sealed class SocketTransport : ITransport
    {
        private static readonly PipeScheduler[] ThreadPoolSchedulerArray = new PipeScheduler[] { PipeScheduler.ThreadPool };

        private readonly MemoryPool<byte> _memoryPool;
        private readonly IEndPointInformation _endPointInformation;
        private readonly IConnectionDispatcher _dispatcher;
        private readonly IApplicationLifetime _appLifetime;
        private readonly int _numSchedulers;
        private readonly PipeScheduler[] _schedulers;
        private readonly ISocketsTrace _trace;
        private Socket _listenSocket;
        private Task _listenTask;
        private Exception _listenException;
        private volatile bool _unbinding;

        internal SocketTransport(
            IEndPointInformation endPointInformation,
            IConnectionDispatcher dispatcher,
            IApplicationLifetime applicationLifetime,
            int ioQueueCount,
            ISocketsTrace trace,
            MemoryPool<byte> memoryPool)
        {
            Debug.Assert(endPointInformation != null);
            Debug.Assert(endPointInformation.Type == ListenType.IPEndPoint);
            Debug.Assert(dispatcher != null);
            Debug.Assert(applicationLifetime != null);
            Debug.Assert(trace != null);

            _endPointInformation = endPointInformation;
            _dispatcher = dispatcher;
            _appLifetime = applicationLifetime;
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
                _numSchedulers = ThreadPoolSchedulerArray.Length;
                _schedulers = ThreadPoolSchedulerArray;
            }
        }

        public Task BindAsync()
        {
            if (_listenSocket != null)
            {
                throw new InvalidOperationException(SocketsStrings.TransportAlreadyBound);
            }

            IPEndPoint endPoint = _endPointInformation.IPEndPoint;

            var listenSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            // Kestrel expects IPv6Any to bind to both IPv6 and IPv4
            if (endPoint.Address == IPAddress.IPv6Any)
            {
                listenSocket.DualMode = true;
            }

            try
            {
                listenSocket.Bind(endPoint);
            }
            catch (SocketException e) when (e.SocketErrorCode == SocketError.AddressAlreadyInUse)
            {
                throw new AddressInUseException(e.Message, e);
            }

            // If requested port was "0", replace with assigned dynamic port.
            if (_endPointInformation.IPEndPoint.Port == 0)
            {
                _endPointInformation.IPEndPoint = (IPEndPoint)listenSocket.LocalEndPoint;
            }

            listenSocket.Listen(512);

            _listenSocket = listenSocket;

            _listenTask = Task.Run(() => RunAcceptLoopAsync());

            return Task.CompletedTask;
        }

        public async Task UnbindAsync()
        {
            if (_listenSocket != null)
            {
                _unbinding = true;
                _listenSocket.Dispose();

                Debug.Assert(_listenTask != null);
                await _listenTask.ConfigureAwait(false);

                _unbinding = false;
                _listenSocket = null;
                _listenTask = null;

                if (_listenException != null)
                {
                    var exInfo = ExceptionDispatchInfo.Capture(_listenException);
                    _listenException = null;
                    exInfo.Throw();
                }
            }
        }

        public Task StopAsync()
        {
            _memoryPool.Dispose();
            return Task.CompletedTask;
        }

        private async Task RunAcceptLoopAsync()
        {
            try
            {
                while (true)
                {
                    for (var schedulerIndex = 0; schedulerIndex < _numSchedulers;  schedulerIndex++)
                    {
                        try
                        {
                            var acceptSocket = await _listenSocket.AcceptAsync();
                            acceptSocket.NoDelay = _endPointInformation.NoDelay;

                            var connection = new SocketConnection(acceptSocket, _memoryPool, _schedulers[schedulerIndex], _trace);

                            // REVIEW: This task should be tracked by the server for graceful shutdown
                            // Today it's handled specifically for http but not for arbitrary middleware
                            _ = HandleConnectionAsync(connection);
                        }
                        catch (SocketException) when (!_unbinding)
                        {
                            _trace.ConnectionReset(connectionId: "(null)");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (_unbinding)
                {
                    // Means we must be unbinding. Eat the exception.
                }
                else
                {
                    _trace.LogCritical(ex, $"Unexpected exception in {nameof(SocketTransport)}.{nameof(RunAcceptLoopAsync)}.");
                    _listenException = ex;

                    // Request shutdown so we can rethrow this exception
                    // in Stop which should be observable.
                    _appLifetime.StopApplication();
                }
            }
        }

        private async Task HandleConnectionAsync(SocketConnection connection)
        {
            try
            {
                var middlewareTask = _dispatcher.OnConnection(connection);
                var transportTask = connection.StartAsync();

                await transportTask;
                await middlewareTask;

                connection.Dispose();
            }
            catch (Exception ex)
            {
                _trace.LogCritical(ex, $"Unexpected exception in {nameof(SocketTransport)}.{nameof(HandleConnectionAsync)}.");
            }
        }
    }
}
