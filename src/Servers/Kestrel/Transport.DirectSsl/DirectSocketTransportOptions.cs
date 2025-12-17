// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Connections;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectSsl;

/// <summary>
/// Options for Direct Socket Transport with integrated OpenSSL TLS handling.
/// </summary>
public class DirectSocketTransportOptions
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
    /// <remarks>
    /// Defaults to 1 MB.
    /// </remarks>
    public long? MaxReadBufferSize { get; set; } = 1024 * 1024;

    /// <summary>
    /// Gets or sets the maximum outgoing bytes the transport will buffer before applying write back-pressure.
    /// </summary>
    /// <remarks>
    /// Defaults to 64 KB.
    /// </remarks>
    public long? MaxWriteBufferSize { get; set; } = 64 * 1024;

    /// <summary>
    /// Inline application and transport continuations instead of dispatching to the thread-pool.
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
    /// Gets or sets the receive buffer size hint.
    /// </summary>
    /// <remarks>
    /// Defaults to null (OS default).
    /// </remarks>
    public int? ReceiveBufferSize { get; set; }

    /// <summary>
    /// Gets or sets the send buffer size hint.
    /// </summary>
    /// <remarks>
    /// Defaults to null (OS default).
    /// </remarks>
    public int? SendBufferSize { get; set; }

    /// <summary>
    /// When connections are closed gracefully, with FIN, we close the write half of the connection first,
    /// and then wait for the read half to close from the client.
    /// However, if the client doesn't close their write half of the connection,
    /// or there was data that the client sent that our application didn't read,
    /// we'll never be able to complete the graceful shutdown.
    /// This timeout controls how long we'll wait for the connection to close gracefully
    /// and if the timeout expires, we'll close the connection abortively.
    /// </summary>
    /// <remarks>
    /// Defaults to 1 second.
    /// </remarks>
    public TimeSpan FinOnErrorTimeout { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// When true, the connection will use FIN instead of RST to close the connection when an error is detected.
    /// </summary>
    public bool FinOnError { get; set; } = false;

    /// <summary>
    /// A function to create the MemoryPool used for buffer management.
    /// </summary>
    public IMemoryPoolFactory<byte>? MemoryPoolFactory { get; set; }

    /// <summary>
    /// A function used to create a new <see cref="Socket"/> to listen with. If not set, a default socket will be created.
    /// </summary>
    public Func<EndPoint, Socket> CreateBoundListenSocket { get; set; } = CreateDefaultBoundListenSocket;

    internal static Socket CreateDefaultBoundListenSocket(EndPoint endpoint)
    {
        Socket listenSocket;

        switch (endpoint)
        {
            case FileHandleEndPoint fileHandle:
                // The listener brokes ownership of the file handle
                listenSocket = new Socket(
                    new SafeSocketHandle((IntPtr)fileHandle.FileHandle, ownsHandle: true)
                );
                break;
            case UnixDomainSocketEndPoint unix:
                listenSocket = new Socket(unix.AddressFamily, SocketType.Stream, ProtocolType.Unspecified);

                // Kestrel expects IPv6Any to bind to both IPv6 and IPv4
                if (listenSocket.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    listenSocket.DualMode = true;
                }

                listenSocket.Bind(endpoint);
                break;
            case IPEndPoint ip:
                listenSocket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                // Kestrel expects IPv6Any to bind to both IPv6 and IPv4
                if (listenSocket.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    listenSocket.DualMode = true;
                }

                listenSocket.Bind(endpoint);
                break;
            default:
                listenSocket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                listenSocket.Bind(endpoint);
                break;
        }

        return listenSocket;
    }
}
