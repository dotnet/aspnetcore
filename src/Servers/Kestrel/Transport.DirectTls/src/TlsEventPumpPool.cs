// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Net;
using System.Net.Security;
using System.Threading.Channels;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls.Connection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls;

/// <summary>
/// Pool of TLS event pumps. Each pump owns a set of connections and handles
/// all TLS I/O for those connections on a dedicated thread.
///
/// With EPOLLEXCLUSIVE, all pumps can accept connections directly, distributing
/// the accept and handshake load across all workers.
/// </summary>
internal sealed class TlsEventPumpPool : IDisposable
{
    private readonly TlsEventPump[] _pumps;
    private readonly ILoggerFactory _loggerFactory;

    public TlsEventPumpPool(int pumpCount, ILoggerFactory loggerFactory, TimeSpan? handshakeTimeout = null)
    {
        _loggerFactory = loggerFactory;

        // Default: 1 pump per CPU core
        pumpCount = pumpCount > 0 ? pumpCount : Environment.ProcessorCount;

        // No timeout by default (used by tests that construct the pool directly); the transport factory
        // always supplies the endpoint's configured HandshakeTimeout.
        var effectiveHandshakeTimeout = handshakeTimeout ?? Timeout.InfiniteTimeSpan;

        _pumps = new TlsEventPump[pumpCount];
        for (int i = 0; i < pumpCount; i++)
        {
            _pumps[i] = new TlsEventPump(loggerFactory.CreateLogger<TlsEventPump>(), i, effectiveHandshakeTimeout);
        }
    }

    /// <summary>
    /// Start all pumps with a listen socket. Each pump registers the listen socket
    /// with EPOLLEXCLUSIVE so that only one pump wakes per incoming connection.
    /// </summary>
    public void StartWithListenSocket(
        int listenFd,
        EndPoint listenEndPoint,
        TlsContext tlsContext,
        Func<ConnectionContext?, string?, (TlsContext Context, RemoteCertificateValidationCallback? ClientCertificateValidation)>? contextResolver,
        ChannelWriter<DirectTlsConnection> readyConnections,
        MemoryPool<byte> memoryPool,
        bool noDelay,
        long maxReadBufferSize,
        long maxWriteBufferSize,
        Action<Exception> onFatalError,
        Action<ConnectionContext, ReadOnlySequence<byte>>? clientHelloCallback = null,
        ConnectionTracker? connectionTracker = null,
        bool serverCertificateSelectorConfigured = true)
    {
        foreach (var pump in _pumps)
        {
            pump.StartWithListenSocket(
                listenFd,
                listenEndPoint,
                tlsContext,
                contextResolver,
                readyConnections,
                memoryPool,
                _loggerFactory,
                noDelay,
                maxReadBufferSize,
                maxWriteBufferSize,
                onFatalError,
                clientHelloCallback,
                connectionTracker,
                serverCertificateSelectorConfigured);
        }
    }

    /// <summary>
    /// Stop every pump from accepting new connections (de-register the listen fd from each pump's epoll
    /// set and clear its cached listen fd). Established connections keep being serviced. Call this before
    /// closing the listen socket. Idempotent.
    /// </summary>
    public void StopAccepting()
    {
        foreach (var pump in _pumps)
        {
            pump.StopAccepting();
        }
    }

    /// <summary>
    /// Stops every pump and asynchronously waits for their threads to exit. Signals all pumps first so their
    /// threads wind down concurrently, then awaits each. Returns <see langword="true"/> only if every pump
    /// thread has actually exited - meaning no pump can touch the shared TLS contexts or memory pool any more,
    /// so the listener may release them. Returns <see langword="false"/> if any pump thread is still running
    /// after its stop timeout (a blocking TLS callback), in which case the listener MUST leak those shared
    /// resources rather than free memory a live pump can still reach. Idempotent.
    /// </summary>
    public async Task<bool> StopAndConfirmExitAsync(CancellationToken cancellationToken)
    {
        // Two phases: signal every pump before awaiting any, so a slow pump doesn't hold back the stop signal
        // to the others. The awaits then overlap, bounding total shutdown by the slowest pump, not their sum.
        foreach (var pump in _pumps)
        {
            pump.SignalStop();
        }

        var stops = new Task<bool>[_pumps.Length];
        for (int i = 0; i < _pumps.Length; i++)
        {
            stops[i] = _pumps[i].StopAndJoinAsync(cancellationToken);
        }

        var results = await Task.WhenAll(stops).ConfigureAwait(false);

        var allExited = true;
        foreach (var exited in results)
        {
            allExited &= exited;
        }

        return allExited;
    }

    public void Dispose() => StopAndConfirmExitAsync(CancellationToken.None).GetAwaiter().GetResult();
}
