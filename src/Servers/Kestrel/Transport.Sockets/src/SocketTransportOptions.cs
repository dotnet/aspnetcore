// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Connections;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets
{
    /// <summary>
    /// Options for socket based transports.
    /// </summary>
    public class SocketTransportOptions
    {
        /// <summary>
        /// The number of I/O queues used to process requests. Set to 0 to directly schedule I/O to the ThreadPool.
        /// </summary>
        /// <remarks>
        /// Defaults to <see cref="Environment.ProcessorCount" /> rounded down and clamped between 1 and 16.
        /// </remarks>
        public int IOQueueCount { get; set; } = Math.Min(Environment.ProcessorCount, 16);

        /// <summary>
        /// Wait until there is data available to allocate a buffer. Setting this to false can increase throughput at the cost of increased memory usage.
        /// </summary>
        /// <remarks>
        /// Defaults to true.
        /// </remarks>
        public bool WaitForDataBeforeAllocatingBuffer { get; set; } = true;

        /// <summary>
        /// Set to false to enable Nagle's algorithm for all connections.
        /// </summary>
        /// <remarks>
        /// Defaults to true.
        /// </remarks>
        public bool NoDelay { get; set; } = true;

        /// <summary>
        /// The maximum length of the pending connection queue.
        /// </summary>
        /// <remarks>
        /// Defaults to 512.
        /// </remarks>
        public int Backlog { get; set; } = 512;

        /// <summary>
        /// Gets or sets the maximum unconsumed incoming bytes the transport will buffer.
        /// </summary>
        public long? MaxReadBufferSize { get; set; } = 1024 * 1024;

        /// <summary>
        /// Gets or sets the maximum outgoing bytes the transport will buffer before applying write backpressure.
        /// </summary>
        public long? MaxWriteBufferSize { get; set; } = 64 * 1024;

        /// <summary>
        /// Inline application and transport continuations instead of dispatching to the threadpool.
        /// </summary>
        /// <remarks>
        /// This will run application code on the IO thread which is why this is unsafe.
        /// It is recommended to set the DOTNET_SYSTEM_NET_SOCKETS_INLINE_COMPLETIONS environment variable to '1' when using this setting to also inline the completions
        /// at the runtime layer as well.
        /// This setting can make performance worse if there is expensive work that will end up holding onto the IO thread for longer than needed.
        /// Test to make sure this setting helps performance.
        /// </remarks>
        public bool UnsafePreferInlineScheduling { get; set; }

        /// <summary>
        /// A function used to create a new <see cref="Socket"/> to listen with. If
        /// not set, <see cref="CreateDefaultListenSocket" /> is used.
        /// </summary>
        public Func<EndPoint, Socket> CreateListenSocket { get; set; } = CreateDefaultListenSocket;

        /// <summary>
        /// Creates a default instance of <see cref="Socket"/> for the given <see cref="EndPoint"/>
        /// that can be used by a connection listener to listen for inbound requests.
        /// </summary>
        /// <param name="endpoint">
        /// An <see cref="EndPoint"/>.
        /// </param>
        /// <returns>
        /// A <see cref="Socket"/> instance.
        /// </returns>
        public static Socket CreateDefaultListenSocket(EndPoint endpoint)
        {
            switch (endpoint)
            {
                case FileHandleEndPoint fileHandle:
                    // We're passing "ownsHandle: true" here even though we don't necessarily
                    // own the handle because Socket.Dispose will clean-up everything safely.
                    // If the handle was already closed or disposed then the socket will
                    // be torn down gracefully, and if the caller never cleans up their handle
                    // then we'll do it for them.
                    //
                    // If we don't do this then we run the risk of Kestrel hanging because the
                    // the underlying socket is never closed and the transport manager can hang
                    // when it attempts to stop.
                    return new Socket(
                        new SafeSocketHandle((IntPtr)fileHandle.FileHandle, ownsHandle: true)
                    );
                case UnixDomainSocketEndPoint unix:
                    return new Socket(unix.AddressFamily, SocketType.Stream, ProtocolType.Unspecified);
                case IPEndPoint ip:
                    var listenSocket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                    // Kestrel expects IPv6Any to bind to both IPv6 and IPv4
                    if (ip.Address == IPAddress.IPv6Any)
                    {
                        listenSocket.DualMode = true;
                    }

                    return listenSocket;
                default:
                    return new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            }
        }

        internal Func<MemoryPool<byte>> MemoryPoolFactory { get; set; } = System.Buffers.PinnedBlockMemoryPoolFactory.Create;
    }
}
