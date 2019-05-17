// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal
{
    internal class ListenerContext
    {
        // REVIEW: This needs to be bounded and we need a strategy for what to do when the queue is full
        private Channel<LibuvConnection> _acceptQueue = Channel.CreateBounded<LibuvConnection>(new BoundedChannelOptions(512)
        {
            // REVIEW: Not sure if this is right as nothing is stopping the libuv callback today
            FullMode = BoundedChannelFullMode.Wait
        });

        public ListenerContext(LibuvTransportContext transportContext)
        {
            TransportContext = transportContext;
        }

        public LibuvTransportContext TransportContext { get; set; }

        public EndPoint EndPoint { get; set; }

        public LibuvThread Thread { get; set; }

        public ValueTask<LibuvConnection> AcceptAsync()
        {
            return _acceptQueue.Reader.ReadAsync();
        }

        /// <summary>
        /// Creates a socket which can be used to accept an incoming connection.
        /// </summary>
        protected UvStreamHandle CreateAcceptSocket()
        {
            switch (EndPoint)
            {
                case IPEndPoint ip:
                    return AcceptTcp();
                case UnixDomainSocketEndPoint domainSocket:
                    return AcceptPipe();
                //case ListenType.FileHandle:
                //    return AcceptHandle();
                default:
                    throw new InvalidOperationException();
            }
        }

        protected async Task HandleConnectionAsync(UvStreamHandle socket)
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

                var connection = new LibuvConnection(socket, TransportContext.Log, Thread, remoteEndPoint, localEndPoint);
                _ = connection.Start();

                await _acceptQueue.Writer.WriteAsync(connection);
            }
            catch (Exception ex)
            {
                TransportContext.Log.LogCritical(ex, $"Unexpected exception in {nameof(ListenerContext)}.{nameof(HandleConnectionAsync)}.");
            }
        }

        private UvTcpHandle AcceptTcp()
        {
            var socket = new UvTcpHandle(TransportContext.Log);

            try
            {
                socket.Init(Thread.Loop, Thread.QueueCloseHandle);
                // socket.NoDelay(EndPointInformation.NoDelay);
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
            _acceptQueue.Writer.Complete();
        }

        // TODO: We need a new custom endpoint
        //private UvStreamHandle AcceptHandle()
        //{
        //    switch (EndPointInformation.HandleType)
        //    {
        //        case FileHandleType.Auto:
        //            throw new InvalidOperationException("Cannot accept on a non-specific file handle, listen should be performed first.");
        //        case FileHandleType.Tcp:
        //            return AcceptTcp();
        //        case FileHandleType.Pipe:
        //            return AcceptPipe();
        //        default:
        //            throw new NotSupportedException();
        //    }
        //}
    }
}
