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
    private readonly ILoggerFactory? _loggerFactory;
    private int _nextPump;

    public TlsEventPumpPool(int pumpCount = 0, ILoggerFactory? loggerFactory = null)
    {
        _loggerFactory = loggerFactory;

        // Default: 1 pump per CPU core
        pumpCount = pumpCount > 0 ? pumpCount : Environment.ProcessorCount;

        _pumps = new TlsEventPump[pumpCount];
        for (int i = 0; i < pumpCount; i++)
        {
            _pumps[i] = new TlsEventPump(loggerFactory?.CreateLogger<TlsEventPump>(), i);
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
        DirectTlsClientHelloBytesCallback? clientHelloCallback = null)
    {
        foreach (var pump in _pumps)
        {
            pump.StartWithListenSocket(
                listenFd,
                tlsContext,
                contextResolver,
                readyConnections,
                memoryPool,
                _loggerFactory!,
                noDelay,
                clientHelloCallback);
        }
    }

    /// <summary>
    /// Returns the next pump in a round-robin fashion.
    /// </summary>
    public TlsEventPump GetNextPump()
    {
        int idx = Interlocked.Increment(ref _nextPump) % _pumps.Length;
        return _pumps[idx];
    }

    public void Dispose()
    {
        foreach (var pump in _pumps)
        {
            pump.Dispose();
        }
    }
}
