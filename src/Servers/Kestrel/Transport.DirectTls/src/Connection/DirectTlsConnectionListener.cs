// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading.Channels;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls.Connection;

/// <summary>
/// DirectTls connection listener that uses EPOLLEXCLUSIVE for worker-based accept.
/// Each pump thread accepts connections directly in its epoll loop, eliminating
/// the accept thread bottleneck and cross-thread handoff overhead.
/// </summary>
internal sealed class DirectTlsConnectionListener : IConnectionListener
{
    private readonly ILogger _logger;
    private readonly DirectTlsTransportOptions _options;

    // Listener-owned pool shared by all of its connections and disposed after its pumps have stopped.
    private readonly MemoryPool<byte> _memoryPool;

    private readonly TlsContext _tlsContext;
    private readonly Func<ConnectionContext?, string?, (TlsContext Context, RemoteCertificateValidationCallback? ClientCertificateValidation)>? _contextResolver;
    private readonly TlsEventPumpPool _pumpPool;
    private readonly Action<ConnectionContext, ReadOnlySequence<byte>>? _clientHelloCallback;

    // Whether the endpoint supplied a ServerCertificateSelector, i.e. whether resolving the TLS context can run user code
    private readonly bool _serverCertificateSelectorConfigured;

    // Native OpenSSL server credentials (bootstrap + per-SNI contexts) owned by this listener. Disposed once,
    // at the end of DisposeAsync, after the pump threads are joined. Null only in tests that don't wire them.
    private readonly IDisposable? _ownedServerContexts;

    private Socket? _listenSocket;
    private int _disposed;

    // Channel for connections that have completed handshake and are ready to be returned
    private readonly Channel<DirectTlsConnection> _readyConnections;

    // Listener-level cap on connections that are handshaking or waiting to be accepted by Kestrel, shared by all pumps.
    private readonly ConnectionTracker _connectionTracker;

    // Host lifetime used to request a graceful shutdown when a pump dies unrecoverably. Never null.
    private readonly IHostApplicationLifetime _appLifetime;

    private int _fatalErrorReported;

    public EndPoint EndPoint { get; private set; }

    public DirectTlsConnectionListener(
        ILoggerFactory loggerFactory,
        TlsContext tlsContext,
        Func<ConnectionContext?, string?, (TlsContext Context, RemoteCertificateValidationCallback? ClientCertificateValidation)>? contextResolver,
        TlsEventPumpPool pumpPool,
        EndPoint endpoint,
        DirectTlsTransportOptions options,
        MemoryPool<byte> memoryPool,
        IHostApplicationLifetime applicationLifetime,
        Action<ConnectionContext, ReadOnlySequence<byte>>? clientHelloCallback = null,
        IDisposable? ownedServerContexts = null,
        bool serverCertificateSelectorConfigured = true)
    {
        ArgumentNullException.ThrowIfNull(tlsContext);
        ArgumentNullException.ThrowIfNull(applicationLifetime);

        _logger = loggerFactory.CreateLogger<DirectTlsConnectionListener>();
        _options = options;
        _memoryPool = memoryPool;

        _pumpPool = pumpPool;
        _tlsContext = tlsContext;
        _contextResolver = contextResolver;
        _clientHelloCallback = clientHelloCallback;
        _serverCertificateSelectorConfigured = serverCertificateSelectorConfigured;
        _ownedServerContexts = ownedServerContexts;
        _appLifetime = applicationLifetime;
        EndPoint = endpoint;

        // Unbounded channel - handshakes complete asynchronously and we don't want to block them
        _readyConnections = Channel.CreateUnbounded<DirectTlsConnection>(new UnboundedChannelOptions
        {
            SingleReader = false,  // Multiple AcceptAsync callers possible
            SingleWriter = false,  // Multiple pump threads write concurrently
        });

        _connectionTracker = new ConnectionTracker(options.MaxConcurrentHandshakes);
    }

    internal void Bind()
    {
        if (_listenSocket is not null)
        {
            throw new InvalidOperationException("Transport already bound");
        }

        Socket listenSocket;
        try
        {
            listenSocket = _options.CreateBoundListenSocket(EndPoint);
        }
        catch (SocketException e) when (e.SocketErrorCode == SocketError.AddressAlreadyInUse)
        {
            throw new AddressInUseException(e.Message, e);
        }

        _listenSocket = listenSocket;

        Debug.Assert(listenSocket.LocalEndPoint != null);
        EndPoint = listenSocket.LocalEndPoint;

        listenSocket.Listen(_options.Backlog);

        // Set the listen socket non-blocking for the EPOLLEXCLUSIVE accept pattern. Each pump waits on epoll
        // and drains the backlog with accept4(); a non-blocking listen fd makes a drained accept return
        // EAGAIN (surfaced as a negative errno by AcceptOne) instead of blocking the pump thread.
        listenSocket.Blocking = false;

        // Start all pump threads with the listen socket (EPOLLEXCLUSIVE)
        // Each pump will accept connections directly in its epoll loop
        int listenFd = (int)listenSocket.Handle;
        _pumpPool.StartWithListenSocket(
            listenFd,
            EndPoint,
            _tlsContext,
            _contextResolver,
            _readyConnections.Writer,
            _memoryPool,
            _options.NoDelay,
            _options.MaxReadBufferSize ?? 0,
            _options.MaxWriteBufferSize ?? 0,
            OnPumpFatalError,
            _clientHelloCallback,
            _connectionTracker,
            _serverCertificateSelectorConfigured);

        _logger.LogInformation("DirectTls listener started with EPOLLEXCLUSIVE worker accept");
    }

    // Invoked by a pump thread when its epoll loop hits an unrecoverable failure. A single dead pump leaves its
    // established connections permanently unserviced while the listen socket and the surviving pumps keep the
    // listener looking healthy, so escalate to a listener-wide fatal error: fault Accept so the connection
    // dispatcher logs the failure, then ask Kestrel to stop the host. Runs at most once per listener.
    internal void OnPumpFatalError(Exception error)
    {
        if (Interlocked.CompareExchange(ref _fatalErrorReported, 1, 0) != 0)
        {
            return;
        }

        _logger.LogCritical(error, "A DirectTls pump thread failed unrecoverably; stopping the application.");

        _readyConnections.Writer.TryComplete(error);
        _appLifetime.StopApplication();
    }

    public async ValueTask<ConnectionContext?> AcceptAsync(CancellationToken cancellationToken = default)
    {
        while (await _readyConnections.Reader.WaitToReadAsync(cancellationToken))
        {
            if (_readyConnections.Reader.TryRead(out var connection))
            {
                _connectionTracker.ReleaseHandshake();
                return connection;
            }
        }

        return null;
    }

    public async ValueTask DisposeAsync()
    {
        // Idempotent: Kestrel disposes a listener once, but guard anyway so the owned native contexts are
        // released exactly once and a stray second call can't double-drain the channel.
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        // Quiesce accept on every pump (de-register the listen fd from their epoll sets and stop the
        // accept loop) BEFORE closing the listen socket. Otherwise a pump can keep calling Accept() on a
        // closed fd - or misroute a client fd that reuses the closed listen fd's number into the accept
        // path - and tight-spin. See TlsEventPump.StopAccepting / AcceptConnections.
        _pumpPool.StopAccepting();
        _listenSocket?.Dispose();
        _readyConnections.Writer.TryComplete();

        // Drain any remaining connections from the channel
        while (_readyConnections.Reader.TryRead(out var connection))
        {
            await connection.DisposeAsync();
        }

        // This listener owns its pump pool; stop the pump threads and release their epoll fds. Bound the wait so
        // a pump stuck in a blocking TLS callback can't hang shutdown: after the budget elapses the pool reports
        // the stuck pump as not-exited and we leak its resources rather than free memory it can still reach.
        using var stopCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var allPumpsExited = await _pumpPool.StopAndConfirmExitAsync(stopCts.Token);

        if (allPumpsExited)
        {
            // Every pump thread has exited, so nothing can touch the shared OpenSSL server credentials
            // (bootstrap + per-SNI contexts) or the memory pool. Release them (idempotent), last so disposal
            // can't race a pump using a context.
            _ownedServerContexts?.Dispose();
            _memoryPool.Dispose();
        }
        else
        {
            // At least one pump thread is still running - a blocking TLS callback (certificate selector,
            // certificate validation, or ClientHello listener) outlived the stop timeout. That thread can still
            // reach the OpenSSL contexts and the memory pool, so freeing them now would be a use-after-free the
            // instant it resumes. Deliberately leak them; the OS reclaims everything when the process exits. The
            // stuck pump already logged a warning.
            _logger.LogWarning(
                "A DirectTls pump thread did not exit during listener shutdown; leaking its OpenSSL contexts and memory pool to avoid a use-after-free. This usually means a TLS certificate, validation, or ClientHello callback is blocking.");
        }
    }

    public ValueTask UnbindAsync(CancellationToken cancellationToken = default)
    {
        // Stop accepting on all pumps and de-register the listen fd from their epoll sets before closing
        // it, so pumps that keep serving established connections don't spin on / misroute the freed fd.
        _pumpPool.StopAccepting();
        _listenSocket?.Dispose();
        _readyConnections.Writer.TryComplete();
        return ValueTask.CompletedTask;
    }
}
