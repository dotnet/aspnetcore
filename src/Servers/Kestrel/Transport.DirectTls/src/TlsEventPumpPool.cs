// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
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
        TlsContext tlsContext,
        Func<ConnectionContext?, string?, (TlsContext Context, RemoteCertificateValidationCallback? ClientCertificateValidation)>? contextResolver,
        ChannelWriter<DirectTlsConnection> readyConnections,
        MemoryPool<byte> memoryPool,
        bool noDelay,
        long maxReadBufferSize,
        long maxWriteBufferSize,
        Action<ConnectionContext, ReadOnlySequence<byte>>? clientHelloCallback = null)
    {
        foreach (var pump in _pumps)
        {
            pump.StartWithListenSocket(
                listenFd,
                tlsContext,
                contextResolver,
                readyConnections,
                memoryPool,
                _loggerFactory,
                noDelay,
                maxReadBufferSize,
                maxWriteBufferSize,
                clientHelloCallback);
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

    public void Dispose()
    {
        foreach (var pump in _pumps)
        {
            pump.Dispose();
        }
    }
}
