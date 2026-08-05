// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Internal;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls;

/// <summary>
/// Transport-wide options for the DirectTls (fd-bound, native OpenSSL) socket transport. Configure with
/// <c>UseDirectTls(options =&gt; ...)</c>. Per-endpoint TLS configuration is supplied separately via
/// <see cref="DirectTlsEndpoint"/> / <see cref="DirectTlsEndpointOptions"/>; this type holds only the
/// transport-wide worker/accept tuning.
/// </summary>
[Experimental("ASPNETCORE_DIRECTTLS_001", UrlFormat = "https://aka.ms/aspnetcore/directtls")]
public sealed class DirectTlsTransportOptions
{
    /// <summary>
    /// The number of TLS worker threads that accept connections and drive TLS handshakes and I/O.
    /// </summary>
    /// <remarks>
    /// Defaults to a value derived from <see cref="Environment.ProcessorCount"/> (see <see cref="DefaultWorkerCount"/>).
    /// </remarks>
    public int WorkerCount { get; set; } = DefaultWorkerCount;

    /// <summary>
    /// The default <see cref="WorkerCount"/>, derived from <see cref="Environment.ProcessorCount"/>.
    /// </summary>
    /// <remarks>
    /// Each worker drives accept, TLS handshakes and I/O for its share of connections, so the worker count bounds
    /// the transport's parallelism. The heuristic mirrors the sockets transport's <c>IOQueue</c> default: capped at
    /// 16 for up to 32 processors, and half the processor count beyond that.
    /// </remarks>
    internal static int DefaultWorkerCount { get; } = DetermineDefaultWorkerCount();

    private static int DetermineDefaultWorkerCount()
    {
        // Each worker schedules the epoll/TLS pump for its connections, so the number of workers determines the
        // maximum parallelism of TLS I/O processing. Mirror the sockets transport IOQueue heuristic: use a
        // high-enough number to not be a significant limiting factor for throughput, without oversubscribing.
        var processorCount = Environment.ProcessorCount;
        if (processorCount <= 32)
        {
            return Math.Min(processorCount, 16);
        }

        return processorCount / 2;
    }

    /// <summary>
    /// Set to false to enable Nagle's algorithm for all connections.
    /// </summary>
    /// <remarks>
    /// Defaults to true.
    /// </remarks>
    internal bool NoDelay { get; set; } = true;

    /// <summary>
    /// The maximum length of the pending connection queue.
    /// </summary>
    /// <remarks>
    /// Defaults to 512 pending connections.
    /// </remarks>
    internal int Backlog { get; set; } = 512;

    /// <summary>
    /// Gets or sets the maximum number of unconsumed inbound (decrypted read) bytes the transport will buffer
    /// before applying backpressure to the peer.
    /// <para>
    /// A value of <see langword="null"/> or 0 disables backpressure entirely, allowing unlimited buffering.
    /// Unlimited server buffering is a security risk given untrusted clients.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Defaults to 1 MiB.
    /// </remarks>
    internal long? MaxReadBufferSize { get; set; } = 1024 * 1024;

    /// <summary>
    /// Gets or sets the maximum number of outbound (encrypted write) bytes the transport will buffer before
    /// applying write backpressure to the application.
    /// <para>
    /// A value of <see langword="null"/> or 0 disables backpressure entirely, allowing unlimited buffering.
    /// Unlimited server buffering is a security risk given untrusted clients.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Defaults to 64 KiB.
    /// </remarks>
    internal long? MaxWriteBufferSize { get; set; } = 64 * 1024;

    internal IMemoryPoolFactory<byte> MemoryPoolFactory { get; set; } = DefaultSimpleMemoryPoolFactory.Instance;

    internal static readonly MemoryPoolOptions MemoryPoolOptions = new() { Owner = "kestrel" };

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
    internal Func<EndPoint, Socket> CreateBoundListenSocket { get; set; } = CreateDefaultBoundListenSocket;

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
    internal static Socket CreateDefaultBoundListenSocket(EndPoint endpoint)
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
        if (endpoint is not FileHandleEndPoint)
        {
            listenSocket.Bind(endpoint);
        }

        return listenSocket;
    }
}
