// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Channels;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls.Connection;
using Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls.Interop;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls;

/// <summary>
/// TLS event pump that handles accept, handshake, and I/O events on a dedicated thread.
/// Uses EPOLLEXCLUSIVE on the listen socket to distribute accept load across workers.
/// </summary>
internal class TlsEventPump : IDisposable
{
    private readonly ILogger? _logger;
    private readonly int _id;

    private readonly int _epollFd;

    // Established connections (handshake complete) - use fd as key
    private readonly ConcurrentDictionary<int, ConnectionIoState> _connections = new();

    // Connections still handshaking - local to pump thread, no sync needed
    private readonly Dictionary<int, HandshakingConnection> _handshaking = new();

    private readonly Thread _pumpThread;
    private volatile bool _running = true;
    private bool _disposed;

    // Listen socket (added with EPOLLEXCLUSIVE). Volatile: written by StopAccepting() (on the disposing
    // thread) and read by the pump thread in PumpLoop/AcceptConnections.
    private volatile int _listenFd = -1;
    // Managed, non-owning wrapper over the listen fd used to call Socket.Accept().
    // The fd is owned by DirectTlsConnectionListener; this wrapper never closes it.
    private Socket? _listenSocket;
    private TlsContext? _tlsContext;
    private Func<ConnectionContext?, string?, (TlsContext Context, RemoteCertificateValidationCallback? ClientCertificateValidation)>? _contextResolver;
    private ChannelWriter<DirectTlsConnection>? _readyConnections;
    private MemoryPool<byte>? _memoryPool;
    private ILoggerFactory? _loggerFactory;
    private bool _noDelay;

    // Optional UseTlsClientHelloListener callback. When set, the raw parsed ClientHello record is
    // handed to it as early as possible - at NeedsTlsContext, right after the session parses the
    // ClientHello and before the real context is installed - not at handshake-complete. Null (the
    // common case) means no capture work is done.
    private Action<ConnectionContext, ReadOnlySequence<byte>>? _clientHelloCallback;

    // Cached loggers for connection creation (initialized in StartWithListenSocket)
    private ILogger<ConnectionIoState>? _connectionIoStateLogger;
    private ILogger<DirectTlsConnection>? _directTlsConnectionLogger;

    // Cached listen endpoint to avoid getsockname syscall per connection
    private EndPoint? _listenEndPoint;

    /// <summary>
    /// Lightweight struct to track TLS connections during handshake.
    /// Uses less memory than ConnectionIoState since we don't need full read/write machinery.
    /// NOTE: We don't create the Socket wrapper - use fd directly to avoid syscall overhead.
    /// </summary>
    private struct HandshakingConnection
    {
        public int Fd;
        public TlsSocketSession Session;
        public System.Net.IPEndPoint? RemoteEndPoint;  // Captured from Socket.RemoteEndPoint at accept time
        public RemoteCertificateValidationCallback? ClientCertificateValidation;  // Endpoint's client-cert validation callback (null when no client cert requested); runs at Complete for mTLS enforcement
        // DirectTlsConnection allocated early (at NeedsTlsContext) so the ClientHello listener has a stable
        // ConnectionContext. Null until the listener fires; reused when the handshake reaches Complete.
        public DirectTlsConnection? Connection;
    }

    public TlsEventPump(ILogger? tlsPumpLogger, int id)
    {
        _id = id;
        _logger = tlsPumpLogger;

        _epollFd = NativeTls.epoll_create1(0);
        if (_epollFd < 0)
        {
            throw new InvalidOperationException($"epoll_create1 failed: {Marshal.GetLastWin32Error()}");
        }

        _pumpThread = new Thread(PumpLoop)
        {
            Name = $"TlsEventPump-{id}",
            IsBackground = true
        };
    }

    /// <summary>
    /// Start the pump with a listen socket. The listen socket is registered with EPOLLEXCLUSIVE
    /// so that only one worker wakes per incoming connection (prevents thundering herd).
    /// </summary>
    public void StartWithListenSocket(
        int listenFd,
        TlsContext tlsContext,
        Func<ConnectionContext?, string?, (TlsContext Context, RemoteCertificateValidationCallback? ClientCertificateValidation)>? contextResolver,
        ChannelWriter<DirectTlsConnection> readyConnections,
        MemoryPool<byte> memoryPool,
        ILoggerFactory loggerFactory,
        bool noDelay,
        Action<ConnectionContext, ReadOnlySequence<byte>>? clientHelloCallback = null)
    {
        _listenFd = listenFd;
        ArgumentNullException.ThrowIfNull(tlsContext);
        _tlsContext = tlsContext;
        _contextResolver = contextResolver;
        _readyConnections = readyConnections;
        _memoryPool = memoryPool;
        _loggerFactory = loggerFactory;
        _noDelay = noDelay;
        _clientHelloCallback = clientHelloCallback;

        // Cache loggers for connection creation
        _connectionIoStateLogger = loggerFactory.CreateLogger<ConnectionIoState>();
        _directTlsConnectionLogger = loggerFactory.CreateLogger<DirectTlsConnection>();

        // Managed, non-owning wrapper over the listen fd. Used both to read the local
        // endpoint once and to accept connections via Socket.Accept() in the pump loop.
        // ownsHandle:false so disposing it never closes the listener-owned fd.
        _listenSocket = new Socket(new SafeSocketHandle((IntPtr)listenFd, ownsHandle: false));
        _listenSocket.Blocking = false;
        _listenEndPoint = _listenSocket.LocalEndPoint;

        // Add listen socket with EPOLLEXCLUSIVE - only one worker wakes per connection
        var ev = new EpollEvent
        {
            Events = NativeTls.EPOLLIN | NativeTls.EPOLLEXCLUSIVE,
            Data = new EpollData { Fd = listenFd }
        };

        int result = NativeTls.epoll_ctl(_epollFd, NativeTls.EPOLL_CTL_ADD, listenFd, ref ev);
        if (result < 0)
        {
            int errno = Marshal.GetLastWin32Error();
            throw new InvalidOperationException($"Failed to add listen socket to epoll: errno={errno}");
        }

        _logger?.LogDebug("Pump {Id}: Added listen socket fd={Fd} with EPOLLEXCLUSIVE", _id, listenFd);

        // Start the pump thread
        _pumpThread.Start();
    }

    public void Unregister(int fd)
    {
        _connections.TryRemove(fd, out _);

        NativeTls.epoll_ctl(_epollFd, NativeTls.EPOLL_CTL_DEL, fd, IntPtr.Zero);
    }

    /// <summary>
    /// Modify the epoll events for a file descriptor.
    /// Used to dynamically add EPOLLOUT when a write would block.
    /// </summary>
    public void ModifyEvents(int fd, uint events)
    {
        // Using level-triggered mode (no EPOLLET) for stability
        var ev = new EpollEvent
        {
            Events = events | NativeTls.EPOLLRDHUP,
            Data = new EpollData { Fd = fd }
        };

        int result = NativeTls.epoll_ctl(_epollFd, NativeTls.EPOLL_CTL_MOD, fd, ref ev);
        if (result < 0)
        {
            int errno = Marshal.GetLastWin32Error();
            _logger?.LogWarning("epoll_ctl MOD failed for fd={Fd}: errno={Errno}", fd, errno);
        }
    }

    // Test-only seams: drive AcceptConnections' guard and loop deterministically without
    // StartWithListenSocket, a real listen socket, epoll registration, or the pump thread.
    internal void SetListenFdForTests(int fd) => _listenFd = fd;
    internal void StopRunningForTests() => _running = false;

    /// <summary>
    /// Stop this pump from accepting new connections: de-register the listen fd from this pump's epoll
    /// set and clear <see cref="_listenFd"/>. Must be called before the listener closes the listen
    /// socket so that (a) the accept loop's guard breaks and (b) a later client fd that reuses the
    /// closed listen fd's number is never misrouted into the accept path by PumpLoop. Idempotent.
    /// Established connections owned by this pump keep being serviced.
    /// </summary>
    internal void StopAccepting()
    {
        int listenFd = _listenFd;
        if (listenFd < 0)
        {
            return;
        }

        // Clear first so PumpLoop's `fd == _listenFd` check and AcceptConnections' guard both stop
        // matching this fd number even before the epoll de-registration below completes.
        _listenFd = -1;

        // epoll_ctl is safe to call concurrently with the pump thread's epoll_wait.
        if (NativeTls.epoll_ctl(_epollFd, NativeTls.EPOLL_CTL_DEL, listenFd, IntPtr.Zero) < 0)
        {
            _logger?.LogDebug("epoll_ctl DEL listenFd={Fd} failed: errno={Errno}", listenFd, Marshal.GetLastWin32Error());
        }
    }

    private void PumpLoop()
    {
        const int MaxEvents = 256;
        var events = new EpollEvent[MaxEvents];

        while (_running)
        {
            // Use shorter timeout when there are handshaking connections
            int timeout = _handshaking.Count > 0 ? 10 : 1000;
            int numEvents = NativeTls.epoll_wait(_epollFd, events, MaxEvents, timeout);

            if (numEvents < 0)
            {
                int errno = Marshal.GetLastWin32Error();
                if (errno == 4)
                {
                    continue; // EINTR
                }
                _logger?.LogError("epoll_wait failed: errno={Errno}", errno);
                break;
            }

            for (int i = 0; i < numEvents; i++)
            {
                int fd = events[i].Data.Fd;
                uint mask = events[i].Events;

                if (fd == 0 && mask == 0)
                {
                    continue;
                }

                // Check if this is the listen socket
                if (fd == _listenFd)
                {
                    AcceptConnections();
                    continue;
                }

                // Check if this is a handshaking connection
                if (_handshaking.TryGetValue(fd, out var handshakingConn))
                {
                    TryAdvanceHandshake(fd, handshakingConn);
                    continue;
                }

                // Check if this is an established connection
                if (!_connections.TryGetValue(fd, out var conn))
                {
                    continue;
                }

                if ((mask & (NativeTls.EPOLLERR | NativeTls.EPOLLHUP)) != 0)
                {
                    // When error events occur, add EPOLLIN|EPOLLOUT
                    // to handle the events in at least one active handler.
                    mask |= NativeTls.EPOLLIN | NativeTls.EPOLLOUT;
                }

                // Process EPOLLIN first - even if EPOLLRDHUP is set, there may be data to read.
                // Read/write drive native SSL_read/SSL_write which can throw on a broken or
                // reset peer; isolate it on the pump thread so one connection cannot crash the
                // process. On failure drop the connection via OnError.
                try
                {
                    if ((mask & NativeTls.EPOLLIN) != 0)
                    {
                        conn.OnReadable();
                    }

                    if ((mask & NativeTls.EPOLLOUT) != 0)
                    {
                        conn.OnWritable();
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogDebug(ex, "Connection I/O threw for fd={Fd}", fd);
                    _connections.TryRemove(fd, out _);
                    conn.OnError(ex);
                    continue;
                }

                // Handle EPOLLRDHUP - peer closed their write side
                if ((mask & NativeTls.EPOLLRDHUP) != 0)
                {
                    if ((mask & NativeTls.EPOLLIN) == 0)
                    {
                        // No data to read, peer closed - signal error
                        _connections.TryRemove(fd, out _);
                        conn.OnError(new IOException("Peer closed connection"));
                    }
                }
            }
        }

        // Cleanup handshaking connections
        foreach (var kvp in _handshaking)
        {
            // Disposing the session closes the underlying socket fd.
            kvp.Value.Session.Dispose();
        }
        _handshaking.Clear();
    }

    /// <summary>
    /// Accept new connections from the listen socket via the managed Socket API.
    /// Drains the accept backlog until <see cref="SocketError.WouldBlock"/>, and stops if the pump is
    /// shutting down (<see cref="_running"/> cleared) or the listen socket has been detached
    /// (<see cref="_listenFd"/> set to -1 by <see cref="StopAccepting"/>).
    /// </summary>
    /// <remarks>
    /// On any accept error we stop the drain and return to <c>epoll_wait</c> rather than looping: the
    /// listen socket is level-triggered, so if connections are still pending we are re-woken immediately,
    /// and once <see cref="StopAccepting"/> has de-registered the fd we are never woken for it again. This
    /// makes the loop spin-proof without a failure counter - a persistently failing accept cannot tight-loop
    /// because a closed listen fd is always de-registered before it is closed. Successful accepts still loop,
    /// so backlog draining under load is unaffected.
    /// </remarks>
    internal void AcceptConnections()
    {
        while (_running && _listenFd >= 0)
        {
            Socket accepted;
            try
            {
                accepted = AcceptOne();
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.WouldBlock)
            {
                // Backlog drained - nothing more to accept right now.
                break;
            }
            catch (ObjectDisposedException)
            {
                // Listen socket wrapper was disposed during shutdown - stop accepting.
                break;
            }
            catch (SocketException ex)
            {
                // Rare accept failure: a per-connection error (e.g. peer reset before accept) or a listen
                // socket torn down mid-drain. Stop this drain and let epoll decide if there is more to do.
                _logger?.LogDebug(ex, "Accept failed: {Error}", ex.SocketErrorCode);
                break;
            }

            ProcessAcceptedSocket(accepted);
        }
    }

    /// <summary>
    /// Accept a single pending connection from the listen socket. Isolated as the sole native accept
    /// call so tests can script accept outcomes without a real listen socket.
    /// </summary>
    internal virtual Socket AcceptOne()
    {
        return _listenSocket!.Accept();
    }

    /// <summary>
    /// Configure a freshly accepted socket, create its TLS session, and register it for handshake
    /// events. Isolated from the accept loop so tests can exercise the loop's control flow without the
    /// native TLS/epoll work.
    /// </summary>
    internal virtual void ProcessAcceptedSocket(Socket accepted)
    {
        // Configure the accepted socket through the managed API: non-blocking so the
        // session can drive readiness via epoll, and TCP_NODELAY for low latency.
        accepted.Blocking = false;
        if (_noDelay)
        {
            accepted.NoDelay = true;
        }

        var remoteEndPoint = accepted.RemoteEndPoint as System.Net.IPEndPoint;
        int clientFd = (int)accepted.Handle;

        // Hand the accepted socket's own SafeSocketHandle to the session. TlsSocketSession
        // takes ownership and disposes it (closing the fd) with the session. Suppress the
        // managed Socket's finalizer so it never double-closes the fd the session now owns.
        var socketHandle = accepted.SafeHandle;
        GC.SuppressFinalize(accepted);

        // Create a socket-bound TLS session and attach the shared server context.
        // SetContext configures SSL_set_fd + server accept state internally.
        var session = new TlsSocketSession(socketHandle);
        try
        {
            session.SetContext(_tlsContext!);
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Failed to initialize TLS session for fd={Fd}", clientFd);
            session.Dispose();
            return;
        }

        // Register client socket with epoll for handshake events
        var ev = new EpollEvent
        {
            Events = NativeTls.EPOLLIN | NativeTls.EPOLLRDHUP,
            Data = new EpollData { Fd = clientFd }
        };

        int result = NativeTls.epoll_ctl(_epollFd, NativeTls.EPOLL_CTL_ADD, clientFd, ref ev);
        if (result < 0)
        {
            int errno = Marshal.GetLastWin32Error();
            _logger?.LogWarning("epoll_ctl ADD failed for handshaking fd={Fd}: errno={Errno}", clientFd, errno);
            session.Dispose();
            return;
        }

        // Track handshaking connection with captured remote endpoint
        _handshaking[clientFd] = new HandshakingConnection
        {
            Fd = clientFd,
            Session = session,
            RemoteEndPoint = remoteEndPoint
        };

        // Try handshake immediately (might complete for resumed sessions)
        TryAdvanceHandshake(clientFd, _handshaking[clientFd]);
    }

    /// <summary>
    /// Try to advance the TLS handshake for a connection.
    /// </summary>
    private void TryAdvanceHandshake(
        int fd,
        HandshakingConnection conn)
    {
        TlsOperationStatus status;
        try
        {
            status = conn.Session.Handshake();
        }
        catch (Exception ex)
        {
            // Under load a peer can reset the socket or send a malformed ClientHello,
            // making SSL_do_handshake fail with a real SSL_ERROR_SSL (surfaced as an
            // AuthenticationException). This runs on the pump thread, so an unhandled
            // throw would tear down the whole process. Isolate it and drop just this
            // connection - one bad client must not affect the others.
            _logger?.LogDebug(ex, "Handshake threw for fd={Fd}", fd);
            DropHandshake(fd, conn);
            return;
        }

        if (status == TlsOperationStatus.Complete)
        {
            // Handshake complete! Create connection and enqueue to Kestrel
            _handshaking.Remove(fd);

            // Mutual TLS (client certificate) handling. The endpoint opts in via
            // HttpsConnectionAdapterOptions.ClientCertificateMode (Allow/Require), which makes
            // CreateStreamTransportOptions set ClientCertificateRequired and install a
            // RemoteCertificateValidationCallback; conn.ClientCertificateValidation carries that callback
            // (null for server-auth-only endpoints, which skip this block entirely). The Linux fd fast
            // handshake path reports Complete directly - it does not surface NeedsCertificateValidation like
            // the buffered PALs do, OpenSSL only enforces SSL_VERIFY_PEER (not FAIL_IF_NO_PEER_CERT), and the
            // fd read/write fast paths bypass the runtime's pending-validation fault. So the runtime cannot
            // enforce the accept/reject decision on this path. The transport runs the endpoint's validation
            // callback here, records the verdict on the session, and tears down rejected connections before
            // they are ever surfaced to Kestrel.
            X509Certificate2? clientCertificate = null;
            if (conn.ClientCertificateValidation is { } validateClientCertificate)
            {
                // The peer's leaf certificate, or null when the client presented none. On the fd fast path
                // this is the runtime's pending external-validation certificate. Intermediates are only
                // fetched when a leaf is present (they feed the chain's ExtraStore). The chain build,
                // policy, and callback invocation live in ClientCertificateValidator so they can be unit
                // tested without epoll or a live session - see its remarks for why AIA downloads are
                // disabled on this pump thread.
                var presentedCertificate = conn.Session.GetRemoteCertificate();
                var intermediates = presentedCertificate is null ? null : conn.Session.GetRemoteCertificates();

                var accepted = ClientCertificateValidator.Validate(conn.Session, presentedCertificate, intermediates, validateClientCertificate);

                if (!accepted)
                {
                    _logger?.LogDebug("Client certificate rejected for fd={Fd} (presented={Presented}).", fd, presentedCertificate is not null);
                    DropHandshake(fd, conn);
                    return;
                }

                // Record the accepted result so the runtime promotes the leaf into its canonical remote-cert
                // slot and clears its pending-validation state.
                try
                {
                    conn.Session.SetRemoteCertificateValidationResult(SslPolicyErrors.None);
                }
                catch (InvalidOperationException)
                {
                    // Validation was already resolved (e.g. a buffered PAL that surfaced
                    // NeedsCertificateValidation before reaching Complete).
                }

                // Surface the accepted certificate to Kestrel via ITlsConnectionFeature. This is the same
                // instance the runtime just promoted into its canonical remote-cert slot (on the accept path
                // SetRemoteCertificateValidationResult moves _externalPendingCert into _remoteCertificate
                // without reallocating), and null when the client presented none on an AllowCertificate
                // endpoint - so we reuse presentedCertificate instead of re-reading it from the session.
                clientCertificate = presentedCertificate;
            }

            // Reuse the DirectTlsConnection allocated early for the ClientHello listener (at
            // NeedsTlsContext), if any, so the connection surfaced to Kestrel keeps the same
            // ConnectionId the listener already observed. Otherwise create both now. Its ConnectionIoState
            // has Pump already set (early path) or set here (default path).
            var earlyConnection = conn.Connection;
            var connectionState = earlyConnection?.ConnectionState
                ?? new ConnectionIoState(fd, conn.Session, _connectionIoStateLogger) { Pump = this };
            connectionState.SetHandshakeComplete();

            // Register with our connections dictionary and epoll
            _connections[fd] = connectionState;

            // Update epoll to use standard connection events (already registered, just confirm)
            var ev = new EpollEvent
            {
                Events = NativeTls.EPOLLIN | NativeTls.EPOLLRDHUP,
                Data = new EpollData { Fd = fd }
            };
            NativeTls.epoll_ctl(_epollFd, NativeTls.EPOLL_CTL_MOD, fd, ref ev);

            // Create DirectTlsConnection using fd directly (no Socket wrapper)
            // This avoids ~5+ syscalls per connection (fstat, getsockopt, fcntl, etc.)
            if (_readyConnections != null && _memoryPool != null)
            {
                DirectTlsConnection directConnection;
                if (earlyConnection is not null)
                {
                    // Promote the early connection: publish the ALPN protocol and validated client cert
                    // that were unknown when it was allocated (the ClientHello listener has already run).
                    directConnection = earlyConnection;
                    directConnection.CompleteHandshake(conn.Session.NegotiatedApplicationProtocol, clientCertificate);
                }
                else
                {
                    directConnection = new DirectTlsConnection(
                        connectionState,
                        this,
                        _listenEndPoint,              // Cached - avoids getsockname syscall
                        conn.RemoteEndPoint,          // Captured from Socket.RemoteEndPoint at accept time
                        _memoryPool,
                        _directTlsConnectionLogger!,
                        negotiatedApplicationProtocol: conn.Session.NegotiatedApplicationProtocol,
                        clientCertificate: clientCertificate);  // Non-null only when the peer presented a client cert (mTLS)
                }

                directConnection.Start();

                if (!_readyConnections.TryWrite(directConnection))
                {
                    // Channel closed (shutting down) - dispose connection
                    directConnection.DisposeAsync().AsTask().GetAwaiter().GetResult();
                }
            }
            else
            {
                // Shutting down before we could surface the connection - release the early one if present.
                earlyConnection?.AbortBeforeStart();
            }
            return;
        }

        if (status == TlsOperationStatus.NeedMoreData)
        {
            // Waiting for more ciphertext from the peer - already registered for EPOLLIN.
            return;
        }

        if (status == TlsOperationStatus.DestinationTooSmall)
        {
            // Need to flush handshake output - add EPOLLOUT.
            var ev = new EpollEvent
            {
                Events = NativeTls.EPOLLIN | NativeTls.EPOLLOUT | NativeTls.EPOLLRDHUP,
                Data = new EpollData { Fd = fd }
            };
            NativeTls.epoll_ctl(_epollFd, NativeTls.EPOLL_CTL_MOD, fd, ref ev);
            return;
        }

        if (status == TlsOperationStatus.NeedsCertificateValidation)
        {
            // Buffered / non-fd PALs surface this suspension so the caller runs client-certificate
            // validation mid-handshake. (The Linux fd fast path our transport uses does not: it reports
            // Complete directly and we validate + surface the certificate in the Complete branch above.)
            // Resolve validation here so the re-driven handshake can finish (accept) or fail (reject); the
            // Complete branch then observes it as already-validated. AcceptWithDefaultValidation runs the
            // default chain build plus the RemoteCertificateValidationCallback configured in
            // HttpsConnectionMiddleware.CreateStreamTransportOptions.
            try
            {
                conn.Session.AcceptWithDefaultValidation();
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "Client certificate validation failed for fd={Fd}", fd);
                DropHandshake(fd, conn);
                return;
            }

            // Re-drive so the handshake completes (accept) or fails (reject).
            TryAdvanceHandshake(fd, conn);
            return;
        }

        if (status == TlsOperationStatus.NeedsTlsContext)
        {
            // Deferred SNI flow: the session parsed the ClientHello and needs the real
            // per-host TLS context before it can continue. Resolve it from the SNI host
            // name and hand it back via SetContext, then re-drive the handshake.
            if (_contextResolver is null)
            {
                // No selector configured but the session still deferred — misconfiguration.
                _logger?.LogDebug("Handshake returned NeedsTlsContext but no certificate resolver is configured for fd={Fd}", fd);
                DropHandshake(fd, conn);
                return;
            }

            // Fire the optional ClientHello listener as early as possible - the session has parsed the
            // ClientHello (which is what produced this NeedsTlsContext suspension), but the real context
            // has not been installed yet and OpenSSL has not run the expensive key exchange / certificate
            // signing. The listener is observable-only today; it does not decide whether the handshake
            // proceeds. Allocate the DirectTlsConnection now (its handshake is not yet complete) so the
            // callback sees the same ConnectionContext / ConnectionId that will later serve the request,
            // then reuse it in the Complete branch. The Connection-is-null guard fires it exactly once even
            // if the handshake needs several more epoll round-trips.
            // Allocate the DirectTlsConnection now (its handshake is not yet complete) so both the
            // certificate selector and the optional ClientHello listener see the same ConnectionContext /
            // ConnectionId that will later serve the request; it is reused in the Complete branch. The
            // Connection-is-null guard makes this run exactly once even if the handshake needs several more
            // epoll round-trips. Because the bootstrap context carries no credentials, every connection
            // reaches NeedsTlsContext, so this early allocation is net-neutral (moved from Complete, not added).
            if (conn.Connection is null && _memoryPool is not null)
            {
                var earlyState = new ConnectionIoState(fd, conn.Session, _connectionIoStateLogger) { Pump = this };
                var earlyConnection = new DirectTlsConnection(
                    earlyState,
                    this,
                    _listenEndPoint,
                    conn.RemoteEndPoint,
                    _memoryPool,
                    _directTlsConnectionLogger!);
                conn.Connection = earlyConnection;
                _handshaking[fd] = conn;

                // Fire the optional ClientHello listener as early as possible - the session has parsed the
                // ClientHello (which is what produced this NeedsTlsContext suspension), but the real context
                // has not been installed yet and OpenSSL has not run the expensive key exchange / certificate
                // signing. The listener is observable-only today; it does not decide whether the handshake proceeds.
                if (_clientHelloCallback is not null)
                {
                    InvokeClientHelloListener(earlyConnection, conn.Session);
                }
            }

            try
            {
                var (resolvedContext, clientCertificateValidation) = _contextResolver(conn.Connection, conn.Session.TargetHostName);
                conn.Session.SetContext(resolvedContext);

                // The endpoint's client-certificate validation callback is resolved with the context. Persist
                // it on the handshaking entry so the Complete branch can drive mTLS validation, even if the
                // handshake needs several more epoll round-trips (each re-reads _handshaking[fd]).
                conn.ClientCertificateValidation = clientCertificateValidation;
                _handshaking[fd] = conn;
            }
            catch (Exception ex)
            {
                // A bad SNI host, a selector that returned no certificate, or a credential
                // acquisition failure must drop only this connection, not the pump.
                _logger?.LogDebug(ex, "SNI certificate resolution failed for fd={Fd}", fd);
                DropHandshake(fd, conn);
                return;
            }

            // Real context is now set; continue the handshake immediately.
            TryAdvanceHandshake(fd, conn);
            return;
        }

        // Handshake failed or connection closed - cleanup.
        _logger?.LogDebug("Handshake failed for fd={Fd}: status={Status}", fd, status);
        DropHandshake(fd, conn);
    }

    // Tears down a handshake that will not complete. Removes the fd from epoll and releases the
    // session. If the ClientHello listener already caused an early DirectTlsConnection to be allocated
    // (at NeedsTlsContext), it is released without the graceful TLS close_notify - a half-open session
    // cannot shut down cleanly, so AbortBeforeStart just completes the (never-started) pipes and closes
    // the socket fd. Otherwise the session is disposed directly, which closes the fd.
    private void DropHandshake(int fd, in HandshakingConnection conn)
    {
        _handshaking.Remove(fd);
        NativeTls.epoll_ctl(_epollFd, NativeTls.EPOLL_CTL_DEL, fd, IntPtr.Zero);
        if (conn.Connection is { } earlyConnection)
        {
            earlyConnection.AbortBeforeStart();
        }
        else
        {
            conn.Session.Dispose();
        }
    }

    // Copies the raw parsed ClientHello record from the session and hands it to the
    // UseTlsClientHelloListener callback. Runs synchronously on the pump (epoll) thread at
    // NeedsTlsContext (before the handshake's key exchange), so the callback must not block. Any
    // failure to capture or a throwing callback is swallowed (logged at debug) - it must never break a
    // connection whose handshake is still in progress.
    private void InvokeClientHelloListener(DirectTlsConnection connection, TlsSocketSession session)
    {
        int length;
        try
        {
            length = session.GetClientHelloLength();
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Failed to read ClientHello length for fd={Fd}", connection.ConnectionId);
            return;
        }

        if (length <= 0)
        {
            return;
        }

        var buffer = ArrayPool<byte>.Shared.Rent(length);
        try
        {
            if (!session.TryGetClientHelloBytes(buffer.AsSpan(0, length), out var written) || written <= 0)
            {
                return;
            }

            // The buffer is only valid for the duration of this synchronous call; it is returned to
            // the pool immediately afterwards. This matches the transient-buffer contract of the
            // socket-transport TlsListener middleware.
            _clientHelloCallback!(connection, new ReadOnlySequence<byte>(buffer, 0, written));
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "TLS ClientHello listener callback threw for fd={Fd}", connection.ConnectionId);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public void Dispose()
    {
        // Dispose must be idempotent: close() is not idempotent, and double-closing _epollFd could close an
        // unrelated fd whose number was recycled between calls. The pump is single-owner (the pool disposes it
        // once), so a plain flag is sufficient.
        if (_disposed)
        {
            return;
        }
        _disposed = true;

        _running = false;
        // IsAlive is false for a never-started thread (tests may construct a pump without starting it)
        // and for an already-finished one; Join would throw on the former, so guard it.
        if (_pumpThread.IsAlive)
        {
            _pumpThread.Join(2000);
        }
        // Non-owning wrapper: disposing it does not close the listener-owned fd.
        _listenSocket?.Dispose();

        // close() is intentionally not retried: on Linux the fd is released even when close returns EINTR, so a
        // retry could close an unrelated fd. A failure here (realistically only EBADF) signals a lifecycle bug
        // rather than a leak, so log it for diagnostics but don't act on it.
        if (NativeTls.close(_epollFd) < 0)
        {
            _logger?.LogDebug("close(epollFd={EpollFd}) failed: errno={Errno}", _epollFd, Marshal.GetLastWin32Error());
        }
    }
}
