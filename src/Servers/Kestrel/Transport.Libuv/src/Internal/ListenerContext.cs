// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal
{
    internal class ListenerContext
    {
        // Single reader, single writer queue since all writes happen from the uv thread and reads happen sequentially
        private readonly Channel<LibuvConnection> _acceptQueue = Channel.CreateUnbounded<LibuvConnection>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        });

        public ListenerContext(LibuvTransportContext transportContext)
        {
            TransportContext = transportContext;
        }

        public LibuvTransportContext TransportContext { get; set; }

        public EndPoint EndPoint { get; set; }

        public LibuvThread Thread { get; set; }

        public PipeOptions InputOptions { get; set; }

        public PipeOptions OutputOptions { get; set; }

        public async ValueTask<LibuvConnection> AcceptAsync(CancellationToken cancellationToken = default)
        {
            while (await _acceptQueue.Reader.WaitToReadAsync(cancellationToken))
            {
                while (_acceptQueue.Reader.TryRead(out var connection))
                {
                    return connection;
                }
            }

            return null;
        }

        /// <summary>
        /// Aborts all unaccepted connections in the queue
        /// </summary>
        /// <returns></returns>
        public async Task AbortQueuedConnectionAsync()
        {
            while (await _acceptQueue.Reader.WaitToReadAsync())
            {
                while (_acceptQueue.Reader.TryRead(out var connection))
                {
                    // REVIEW: Pass an abort reason?
                    connection.Abort();
                }
            }
        }

        /// <summary>
        /// Creates a socket which can be used to accept an incoming connection.
        /// </summary>
        protected UvStreamHandle CreateAcceptSocket()
        {
            switch (EndPoint)
            {
                case IPEndPoint _:
                    return AcceptTcp();
                case UnixDomainSocketEndPoint _:
                    return AcceptPipe();
                case FileHandleEndPoint _:
                    return AcceptHandle();
                default:
                    throw new InvalidOperationException();
            }
        }

        protected internal void HandleConnection(UvStreamHandle socket)
        {
            try
            {
                IPEndPoint remoteEndPoint = null;
                IPEndPoint localEndPoint = null;

                if (socket is UvTcpHandle tcpHandle)
                {
                    try
                    {
                        remoteEndPoint = tcpHandle.GetPeerIPEndPoint();
                        localEndPoint = tcpHandle.GetSockIPEndPoint();
                    }
                    catch (UvException ex) when (LibuvConstants.IsConnectionReset(ex.StatusCode))
                    {
                        TransportContext.Log.ConnectionReset("(null)");
                        socket.Dispose();
                        return;
                    }
                }

                var options = TransportContext.Options;
#pragma warning disable CS0618
                var connection = new LibuvConnection(socket, TransportContext.Log, Thread, remoteEndPoint, localEndPoint, InputOptions, OutputOptions, options.MaxReadBufferSize, options.MaxWriteBufferSize);
#pragma warning restore CS0618
                connection.Start();

                bool accepted = _acceptQueue.Writer.TryWrite(connection);
                Debug.Assert(accepted, "The connection was not written to the channel!");
            }
            catch (Exception ex)
            {
                TransportContext.Log.LogCritical(ex, $"Unexpected exception in {nameof(ListenerContext)}.{nameof(HandleConnection)}.");
            }
        }

        private UvTcpHandle AcceptTcp()
        {
            var socket = new UvTcpHandle(TransportContext.Log);

            try
            {
                socket.Init(Thread.Loop, Thread.QueueCloseHandle);
#pragma warning disable CS0618
                socket.NoDelay(TransportContext.Options.NoDelay);
#pragma warning restore CS0618
            }
            catch
            {
                socket.Dispose();
                throw;
            }

            return socket;
        }

        private UvPipeHandle AcceptPipe()
        {
            var pipe = new UvPipeHandle(TransportContext.Log);

            try
            {
                pipe.Init(Thread.Loop, Thread.QueueCloseHandle);
            }
            catch
            {
                pipe.Dispose();
                throw;
            }

            return pipe;
        }

        protected void StopAcceptingConnections()
        {
            _acceptQueue.Writer.TryComplete();
        }

        private UvStreamHandle AcceptHandle()
        {
            var fileHandleEndPoint = (FileHandleEndPoint)EndPoint;

            switch (fileHandleEndPoint.FileHandleType)
            {
                case FileHandleType.Auto:
                    throw new InvalidOperationException("Cannot accept on a non-specific file handle, listen should be performed first.");
                case FileHandleType.Tcp:
                    return AcceptTcp();
                case FileHandleType.Pipe:
                    return AcceptPipe();
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
