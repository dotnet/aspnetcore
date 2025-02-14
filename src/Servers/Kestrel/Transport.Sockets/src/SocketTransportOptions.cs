// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Connections;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;

/// <summary>
/// Options for socket based transports.
/// </summary>
public class SocketTransportOptions
{
    private const string FinOnErrorSwitch = "Microsoft.AspNetCore.Server.Kestrel.FinOnError";
    private static readonly bool _finOnError;

    static SocketTransportOptions()
    {
        AppContext.TryGetSwitch(FinOnErrorSwitch, out _finOnError);
    }

    // Opt-out flag for back compat. Remove in 9.0 (or make public).
    internal bool FinOnError { get; set; } = _finOnError;

    /// <summary>
    /// The number of I/O queues used to process requests. Set to 0 to directly schedule I/O to the ThreadPool.
    /// </summary>
    /// <remarks>
    /// Defaults to a value based on and limited to <see cref="Environment.ProcessorCount" />.
    /// </remarks>
    public int IOQueueCount { get; set; } = Internal.IOQueue.DefaultCount;

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
    /// Defaults to 512 pending connections.
    /// </remarks>
    public int Backlog { get; set; } = 512;

    /// <summary>
    /// Gets or sets the maximum unconsumed incoming bytes the transport will buffer.
    /// <para>
    /// A value of <see langword="null"/> or 0 disables backpressure entirely allowing unlimited buffering.
    /// Unlimited server buffering is a security risk given untrusted clients.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Defaults to 1 MiB.
    /// </remarks>
    public long? MaxReadBufferSize { get; set; } = 1024 * 1024;

    /// <summary>
    /// Gets or sets the maximum outgoing bytes the transport will buffer before applying write backpressure.
    /// <para>
    /// A value of <see langword="null"/> or 0 disables backpressure entirely allowing unlimited buffering.
    /// Unlimited server buffering is a security risk given untrusted clients.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Defaults to 64 KiB.
    /// </remarks>
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
    /// <remarks>
    /// Defaults to false.
    /// </remarks>
    public bool UnsafePreferInlineScheduling { get; set; }

    /// <summary>
    /// A function used to create a new <see cref="Socket"/> to listen with. If
    /// not set, <see cref="CreateDefaultBoundListenSocket" /> is used.
    /// </summary>
    /// <remarks>
    /// Implementors are expected to call <see cref="Socket.Bind"/> on the
    /// <see cref="Socket"/>. Please note that <see cref="CreateDefaultBoundListenSocket"/>
    /// calls <see cref="Socket.Bind"/> as part of its implementation, so implementors
    /// using this method do not need to call it again.
    /// </remarks>
    /// <remarks>
    /// Defaults to <see cref="CreateDefaultBoundListenSocket"/>.
    /// </remarks>
    public Func<EndPoint, Socket> CreateBoundListenSocket { get; set; } = CreateDefaultBoundListenSocket;

    /// <summary>
    /// Creates a default instance of <see cref="Socket"/> for the given <see cref="EndPoint"/>
    /// that can be used by a connection listener to listen for inbound requests. <see cref="Socket.Bind"/>
    /// is called by this method.
    /// </summary>
    /// <param name="endpoint">
    /// An <see cref="EndPoint"/>.
    /// </param>
    /// <returns>
    /// A <see cref="Socket"/> instance.
    /// </returns>
    public static Socket CreateDefaultBoundListenSocket(EndPoint endpoint)
    {
        Socket listenSocket;
        switch (endpoint)
        {
            case FileHandleEndPoint fileHandle:
                // We're passing "ownsHandle: false" to avoid side-effects on the
                // handle when disposing the socket.
                //
                // When the non-owning SafeSocketHandle gets disposed (on .NET 7+),
                // on-going async operations are aborted.
                listenSocket = new Socket(
                    new SafeSocketHandle((IntPtr)fileHandle.FileHandle, ownsHandle: false)
                );
                break;
            case UnixDomainSocketEndPoint unix:
                listenSocket = new Socket(unix.AddressFamily, SocketType.Stream, ProtocolType.Unspecified);
                break;
            case IPEndPoint ip:
                listenSocket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                // Kestrel expects IPv6Any to bind to both IPv6 and IPv4
                if (ip.Address.Equals(IPAddress.IPv6Any))
                {
                    listenSocket.DualMode = true;
                }

                break;
            default:
                listenSocket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                break;
        }

        // we only call Bind on sockets that were _not_ created
        // using a file handle; the handle is already bound
        // to an underlying socket so doing it again causes the
        // underlying PAL call to throw
        if (!(endpoint is FileHandleEndPoint))
        {
            listenSocket.Bind(endpoint);
        }

        return listenSocket;
    }

    internal Func<MemoryPool<byte>> MemoryPoolFactory { get; set; } = System.Buffers.PinnedBlockMemoryPoolFactory.Create;
}
