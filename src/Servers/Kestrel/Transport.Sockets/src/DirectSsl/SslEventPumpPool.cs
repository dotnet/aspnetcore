// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Threading.Channels;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl.Connection;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl.Ssl;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl;

/// <summary>
/// Pool of SSL event pumps. Each pump owns a set of connections and handles
/// all SSL I/O for those connections on a dedicated thread.
/// 
/// With EPOLLEXCLUSIVE, all pumps can accept connections directly, distributing
/// the accept and handshake load across all workers.
/// </summary>
internal sealed class SslEventPumpPool : IDisposable
{
    private readonly SslEventPump[] _pumps;
    private readonly ILoggerFactory? _loggerFactory;
    private int _nextPump;

    public SslEventPumpPool(int pumpCount = 0, ILoggerFactory? loggerFactory = null)
    {
        _loggerFactory = loggerFactory;
        
        // Default: 1 pump per CPU core
        pumpCount = pumpCount > 0 ? pumpCount : Environment.ProcessorCount;

        _pumps = new SslEventPump[pumpCount];
        for (int i = 0; i < pumpCount; i++)
        {
            _pumps[i] = new SslEventPump(loggerFactory?.CreateLogger<SslEventPump>(), i);
        }
    }

    /// <summary>
    /// Start all pumps with a listen socket. Each pump registers the listen socket
    /// with EPOLLEXCLUSIVE so that only one pump wakes per incoming connection.
    /// </summary>
    public void StartWithListenSocket(
        int listenFd,
        SslContext sslContext,
        ChannelWriter<DirectSslConnection> readyConnections,
        MemoryPool<byte> memoryPool,
        bool noDelay)
    {
        foreach (var pump in _pumps)
        {
            pump.StartWithListenSocket(
                listenFd, 
                sslContext.Handle, 
                readyConnections, 
                memoryPool, 
                _loggerFactory!,
                noDelay);
        }
    }

    /// <summary>
    /// Returns the next pump in a round-robin fashion.
    /// </summary>
    public SslEventPump GetNextPump()
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
