// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
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
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls;

/// <summary>
/// TLS event pump that handles accept, handshake, and I/O events on a dedicated thread.
/// Uses EPOLLEXCLUSIVE on the listen socket to distribute accept load across workers.
/// </summary>
internal class TlsEventPump : IDisposable
{
    private readonly ILogger _logger;
    private readonly int _id;

    // Maximum time a connection is allowed to spend handshaking before the pump drops it. Stored as
    // milliseconds for cheap comparison against Environment.TickCount64. long.MaxValue means "no timeout"
    // (Timeout.InfiniteTimeSpan / TimeSpan.MaxValue) and is never enforced.
    private readonly long _handshakeTimeoutMs;

    private readonly int _epollFd;

    private const uint DefaultEpollInterest = NativeTls.EPOLLIN | NativeTls.EPOLLRDHUP;

    // Established connections (handshake complete) - use fd as key
    private readonly ConcurrentDictionary<int, ConnectionIoState> _connections = new();

    // Connections still handshaking - local to pump thread, no sync needed
    private readonly Dictionary<int, HandshakingConnection> _handshaking = new();

    private readonly Thread _pumpThread;
    private volatile bool _running = true;

    // Completed by the pump thread as the very last thing it does in PumpLoop's finally, after it has released
    // its handshakes and closed its own epoll fd - so awaiting it is proof the thread can no longer reach the
    // epoll fd, the TLS contexts, or the memory pool. RunContinuationsAsynchronously keeps StopAndJoinAsync's
    // continuation off the pump thread. Set true just before the thread is started so a never-started pump
    // (constructed by tests) can be detected and short-circuited.
    private readonly TaskCompletionSource _exitSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool _threadStarted;
    // Guards the one-time epoll fd close, which happens either in the pump thread's finally (started pump) or
    // in StopAndJoinAsync (never-started pump) - never both. Interlocked so a double close can't hit an
    // unrelated fd whose number was recycled.
    private int _epollClosed;
    // Memoizes StopAndJoinAsync so repeated/concurrent stop calls observe one shutdown, not a re-run.
    private readonly object _stopLock = new();
    private Task<bool>? _stopTask;

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
    private ILoggerFactory _loggerFactory = NullLoggerFactory.Instance;
    private bool _noDelay;
    // Writer backpressure thresholds for the per-connection duplex pipes (0 = unbounded). Sourced from
    // DirectTlsTransportOptions.MaxReadBufferSize / MaxWriteBufferSize and applied in the DirectTlsConnection ctor.
    private long _maxReadBufferSize;
    private long _maxWriteBufferSize;

    // Optional UseTlsClientHelloListener callback. When set, the raw parsed ClientHello record is
    // handed to it as early as possible - at NeedsTlsContext, right after the session parses the
    // ClientHello and before the real context is installed - not at handshake-complete. Null (the
    // common case) means no capture work is done.
    private Action<ConnectionContext, ReadOnlySequence<byte>>? _clientHelloCallback;

    // Cached loggers for connection creation (initialized in StartWithListenSocket)
    private ILogger<ConnectionIoState> _connectionIoStateLogger = NullLogger<ConnectionIoState>.Instance;
    private ILogger<DirectTlsConnection> _directTlsConnectionLogger = NullLogger<DirectTlsConnection>.Instance;

    // Listener-level connection tracker shared by all pumps of this listener. Always non-null: defaults to the
    // disabled ConnectionTracker.Unlimited (no-op acquire/release) until StartWithListenSocket supplies the
    // listener's tracker. When a MaxConcurrentHandshakes cap is configured, a freshly accepted connection is
    // rejected (its fd closed before the handshake) once the in-flight handshake count reaches the cap.
    private ConnectionTracker _connectionTracker = ConnectionTracker.Unlimited;

    // Cached listen endpoint to avoid getsockname syscall per connection
    private EndPoint? _listenEndPoint;

    /// <summary>
    /// Lightweight struct to track TLS connections during handshake.
    /// Uses less memory than ConnectionIoState since we don't need full read/write machinery.
    /// NOTE: We don't create the Socket wrapper - use fd directly to avoid syscall overhead.
    /// </summary>
    internal struct HandshakingConnection
    {
        public int Fd;
        public TlsSocketSession Session;
        /// <summary>
        /// Captured from Socket.RemoteEndPoint at accept time
        /// </summary>
        public IPEndPoint? RemoteEndPoint;
        /// <summary>
        /// Endpoint's client-cert validation callback (null when no client cert requested); runs at Complete for mTLS enforcement
        /// </summary>
        public RemoteCertificateValidationCallback? ClientCertificateValidation;
        /// <summary>
        /// DirectTlsConnection allocated early (at NeedsTlsContext) so the ClientHello listener has a stable
        /// ConnectionContext. Null until the listener fires; reused when the handshake reaches Complete.
        /// </summary>
        public DirectTlsConnection? Connection;
        /// <summary>
        /// Environment.TickCount64 value at/after which this handshake is considered timed out and dropped.
        /// long.MaxValue means the handshake never times out (timeouts disabled for this pump).
        /// </summary>
        public long HandshakeDeadlineTimestamp;
        /// <summary>
        /// The fd's current epoll interest set, mirrored from the last epoll_ctl issued for this handshaking socket.
        /// </summary>
        public uint CurrentEpollInterest;
    }

    public TlsEventPump(ILogger tlsPumpLogger, int id, TimeSpan handshakeTimeout)
    {
        _id = id;
        _logger = tlsPumpLogger;
        _handshakeTimeoutMs = handshakeTimeout == Timeout.InfiniteTimeSpan || handshakeTimeout == TimeSpan.MaxValue
            ? long.MaxValue
            : (long)handshakeTimeout.TotalMilliseconds;

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
        long maxReadBufferSize,
        long maxWriteBufferSize,
        Action<ConnectionContext, ReadOnlySequence<byte>>? clientHelloCallback = null,
        ConnectionTracker? connectionTracker = null)
    {
        _listenFd = listenFd;
        ArgumentNullException.ThrowIfNull(tlsContext);
        _tlsContext = tlsContext;
        _contextResolver = contextResolver;
        _readyConnections = readyConnections;
        _memoryPool = memoryPool;
        _loggerFactory = loggerFactory;
        _noDelay = noDelay;
        _maxReadBufferSize = maxReadBufferSize;
        _maxWriteBufferSize = maxWriteBufferSize;
        _clientHelloCallback = clientHelloCallback;
        _connectionTracker = connectionTracker ?? ConnectionTracker.Unlimited;

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

        _logger.LogDebug("Pump {Id}: Added listen socket fd={Fd} with EPOLLEXCLUSIVE", _id, listenFd);

        // Set before Start so a stop that races startup still awaits the exit signal instead of taking the
        // never-started fast path (which would close the epoll fd underneath the just-launched thread).
        _threadStarted = true;

        // Start the pump thread
        _pumpThread.Start();
    }

    public void Unregister(int fd) => DropEstablishedConnection(fd);

    // Remove an established connection from the pump: drop it from the connection table AND de-register its
    // fd from this pump's epoll set. Both halves are required. Established-connection events are
    // level-triggered, so an fd left registered after the connection is gone keeps re-firing on every
    // epoll_wait; it is then dropped again at the _connections lookup in HandleConnectionEvent, which
    // tight-spins the pump thread at 100% CPU and starves every other connection this pump owns.
    private void DropEstablishedConnection(int fd)
    {
        _connections.TryRemove(fd, out _);
        DeregisterFromEpoll(fd);
    }

    // The single epoll de-registration syscall, isolated as a virtual seam (like RawRead/AcceptOne) so
    // tests can observe which fds the pump removes from its interest set without a live epoll instance.
    internal virtual void DeregisterFromEpoll(int fd)
        => NativeTls.epoll_ctl(_epollFd, NativeTls.EPOLL_CTL_DEL, fd, IntPtr.Zero);

    // Issues the EPOLL_CTL_MOD syscall for a handshaking fd. Callers should go through SetHandshakeInterest so
    // the cached CurrentEpollInterest stays in sync. internal and virtual for testing.
    internal virtual void UpdateHandshakeInterest(int fd, uint events)
    {
        var ev = new EpollEvent
        {
            Events = events,
            Data = new EpollData { Fd = fd }
        };
        NativeTls.epoll_ctl(_epollFd, NativeTls.EPOLL_CTL_MOD, fd, ref ev);
    }

    // The single choke point for changing a handshaking fd's epoll interest set: rewrites the kernel interest
    // (via the UpdateHandshakeInterest seam) AND records the new mask on the handshaking entry so
    // CurrentEpollInterest always mirrors what the kernel is subscribed to. Every mid-handshake interest
    // change must go through here so the cached mask cannot drift.
    private void SetHandshakeInterest(int fd, ref HandshakingConnection conn, uint events)
    {
        UpdateHandshakeInterest(fd, events);
        conn.CurrentEpollInterest = events;
        _handshaking[fd] = conn;
    }

    /// <summary>
    /// Modify the epoll events for a file descriptor.
    /// Used to dynamically add EPOLLOUT when a write would block.
    /// </summary>
    public void ModifyEvents(int fd, uint events)
    {
        // Level-triggered mode (no EPOLLET) for stability. EPOLLRDHUP (peer half-close) rides with read interest:
        // arm it only when EPOLLIN is requested, so a connection whose read interest is suspended for backpressure
        // isn't torn down by a lone EPOLLRDHUP while it still has buffered request data left to drain.
        if ((events & NativeTls.EPOLLIN) != 0)
        {
            events |= NativeTls.EPOLLRDHUP;
        }

        var ev = new EpollEvent
        {
            Events = events,
            Data = new EpollData { Fd = fd }
        };

        int result = NativeTls.epoll_ctl(_epollFd, NativeTls.EPOLL_CTL_MOD, fd, ref ev);
        if (result < 0)
        {
            int errno = Marshal.GetLastWin32Error();
            _logger.LogWarning("epoll_ctl MOD failed for fd={Fd}: errno={Errno}", fd, errno);
        }
    }

    // internal for testing
    internal void SetListenFd(int fd) => _listenFd = fd;
    internal void StopRunning() => _running = false;
    internal Dictionary<int, HandshakingConnection> Handshakes => _handshaking;
    internal bool IsHandshaking(int fd) => _handshaking.ContainsKey(fd);

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
            _logger.LogDebug("epoll_ctl DEL listenFd={Fd} failed: errno={Errno}", listenFd, Marshal.GetLastWin32Error());
        }
    }

    private void PumpLoop()
    {
        const int MaxEvents = 256;
        var events = new NativeTls.EpollEventBuffer(MaxEvents);

        try
        {
            while (_running)
            {
                try
                {
                    // Use shorter timeout when there are handshaking connections
                    int timeout = _handshaking.Count > 0 ? 10 : 1000;
                    int numEvents = events.Wait(_epollFd, timeout);

                    if (numEvents < 0)
                    {
                        int errno = Marshal.GetLastWin32Error();
                        if (errno == 4)
                        {
                            continue; // EINTR
                        }
                        _logger.LogError("epoll_wait failed: errno={Errno}", errno);
                        break;
                    }

                    for (int i = 0; i < numEvents; i++)
                    {
                        var epollEvent = events[i];
                        int fd = epollEvent.Data.Fd;
                        uint mask = epollEvent.Events;

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

                        // Established connection - dispatch its I/O. Extracted so the failure/drop paths are testable.
                        HandleConnectionEvent(fd, mask);
                    }

                    // Drop connections whose handshake has taken too long. The epoll_wait timeout above is 10ms
                    // while any handshake is in flight, so a stalled handshake (e.g. a slow-loris ClientHello) is
                    // swept within ~10ms of its deadline even when the connection sends nothing to wake the pump.
                    if (_handshakeTimeoutMs != long.MaxValue && _handshaking.Count > 0)
                    {
                        SweepExpiredHandshakes(Environment.TickCount64);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Pump {Id} encountered an exception in PumpLoop", _id);
                }
            }
        }
        finally
        {
            // The thread owns its own teardown: release half-open handshakes, then close the epoll fd it created
            // in the constructor, and only then signal exit. Ordering matters - _exitSignal is the proof
            // StopAndJoinAsync waits on before the listener frees the TLS contexts and memory pool, so it must be
            // the last thing this thread does after every resource access here. In a finally so a stray escape
            // (or a break above) still signals, otherwise the awaiter would hang until its timeout and leak.
            ReleasePendingHandshakes();
            CloseEpollFd();
            _exitSignal.TrySetResult();
        }
    }

    // Release every half-open handshake after the event loop stops. Use the same ownership-aware teardown as
    // ordinary handshake failures so a DirectTlsConnection allocated at NeedsTlsContext is aborted as well.
    internal void ReleasePendingHandshakes()
    {
        foreach (var connection in _handshaking.Values)
        {
            ReleaseHandshakeResources(connection);
        }

        _handshaking.Clear();
    }

    // Dispatches an epoll event for an established (post-handshake) connection. Extracted from PumpLoop's
    // event loop so the failure paths (I/O throw, peer RDHUP) can be driven directly from tests.
    internal void HandleConnectionEvent(int fd, uint mask)
    {
        if (!_connections.TryGetValue(fd, out var conn))
        {
            return;
        }

        if ((mask & (NativeTls.EPOLLERR | NativeTls.EPOLLHUP)) != 0)
        {
            // When error events occur, add EPOLLIN|EPOLLOUT to handle the events in at least one active handler.
            mask |= NativeTls.EPOLLIN | NativeTls.EPOLLOUT;
        }

        // Process EPOLLIN first - even if EPOLLRDHUP is set, there may be data to read. Read/write drive
        // native SSL_read/SSL_write which can throw on a broken or reset peer; isolate it on the pump thread
        // so one connection cannot crash the process. On failure drop the connection via OnError.
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
            _logger.LogDebug(ex, "Connection I/O threw for fd={Fd}", fd);
            DropEstablishedConnection(fd);
            conn.OnError(ex);
            return;
        }

        // Handle EPOLLRDHUP - peer closed their write side.
        if ((mask & NativeTls.EPOLLRDHUP) != 0)
        {
            if ((mask & NativeTls.EPOLLIN) == 0)
            {
                // No data to read, peer closed - signal error.
                DropEstablishedConnection(fd);
                conn.OnError(new IOException("Peer closed connection"));
            }
        }
    }

    // internal for testing: seed an established connection without running the native handshake.
    internal void TrackConnectionForTest(int fd, ConnectionIoState conn) => _connections[fd] = conn;

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
                _logger.LogDebug(ex, "Accept failed: {Error}", ex.SocketErrorCode);
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

        var remoteEndPoint = accepted.RemoteEndPoint as IPEndPoint;

        // TlsSocketSession takes ownership of its SafeSocketHandle. Explicitly transfer the fd out of the
        // accepted Socket: suppressing the Socket finalizer would leave the Socket undisposed and make the
        // lifetime depend on an implicit shared SafeSocketHandle reference.
        var socketHandle = TransferSocketHandleOwnership(accepted);
        int clientFd = (int)socketHandle.DangerousGetHandle();

        // Create a socket-bound TLS session and attach the shared server context.
        // SetContext configures SSL_set_fd + server accept state internally.
        var session = new TlsSocketSession(socketHandle);
        try
        {
            session.SetContext(_tlsContext!);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to initialize TLS session for fd={Fd}", clientFd);
            session.Dispose();
            return;
        }

        // Register client socket with epoll for handshake events
        var ev = new EpollEvent
        {
            Events = DefaultEpollInterest,
            Data = new EpollData { Fd = clientFd }
        };

        int result = NativeTls.epoll_ctl(_epollFd, NativeTls.EPOLL_CTL_ADD, clientFd, ref ev);
        if (result < 0)
        {
            int errno = Marshal.GetLastWin32Error();
            _logger.LogWarning("epoll_ctl ADD failed for handshaking fd={Fd}: errno={Errno}", clientFd, errno);
            session.Dispose();
            return;
        }

        // match Kestrel's MaxConcurrentConnections: accept, but if over limit reject the connection.
        if (!_connectionTracker.TryAcquireHandshake())
        {
            _logger.LogDebug("Rejecting fd={Fd}: in-flight connection cap reached", clientFd);
            NativeTls.epoll_ctl(_epollFd, NativeTls.EPOLL_CTL_DEL, clientFd, IntPtr.Zero);
            session.Dispose();
            return;
        }

        // Track handshaking connection with captured remote endpoint. CurrentEpollInterest mirrors the ADD
        // above so later steps can reconcile the interest set (arm/clear EPOLLOUT) without a redundant syscall.
        _handshaking[clientFd] = new HandshakingConnection
        {
            Fd = clientFd,
            Session = session,
            RemoteEndPoint = remoteEndPoint,
            HandshakeDeadlineTimestamp = ComputeHandshakeDeadline(Environment.TickCount64),
            CurrentEpollInterest = DefaultEpollInterest,
        };

        // Try handshake immediately (might complete for resumed sessions)
        TryAdvanceHandshake(clientFd, _handshaking[clientFd]);
    }

    /// <summary>
    /// Transfers ownership of <paramref name="socket"/>'s native handle to a new owning
    /// <see cref="SafeSocketHandle"/> and disposes the original <see cref="Socket"/> without closing the handle.
    /// </summary>
    /// <remarks>
    /// The caller owns the returned handle and must dispose it or transfer ownership to another owner.
    /// </remarks>
    internal static SafeSocketHandle TransferSocketHandleOwnership(Socket socket)
    {
        ArgumentNullException.ThrowIfNull(socket);

        var socketHandle = socket.SafeHandle;
        var transferredHandle = new SafeSocketHandle(socketHandle.DangerousGetHandle(), ownsHandle: true);
        socketHandle.SetHandleAsInvalid();
        socket.Dispose();

        return transferredHandle;
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
            _logger.LogDebug(ex, "Handshake threw for fd={Fd}", fd);
            DropHandshake(fd, conn);
            return;
        }

        if (status == TlsOperationStatus.Complete)
        {
            // Handshake complete: validate any client certificate, build the connection, and promote the fd from handshaking to established.
            X509Certificate2? clientCertificate = null;
            var earlyConnection = conn.Connection;
            ConnectionIoState connectionState;
            DirectTlsConnection directConnection;

            try
            {
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
                        _logger.LogDebug("Client certificate rejected for fd={Fd} (presented={Presented}).", fd, presentedCertificate is not null);
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

                // Both are set before the pump thread starts and never cleared, so this is unreachable;
                if (_readyConnections is null || _memoryPool is null)
                {
                    Debug.Assert(false, "Handshake completed before the pump was initialized.");
                    _logger.LogWarning("fd={Fd}: handshake completed before the pump was initialized; dropping.", fd);
                    DropHandshake(fd, conn);
                    return;
                }

                // Reuse the DirectTlsConnection allocated early for the ClientHello listener (at
                // NeedsTlsContext), if any, so the connection surfaced to Kestrel keeps the same
                // ConnectionId the listener already observed. Otherwise create both now. Its ConnectionIoState
                // has Pump already set (early path) or set here (default path).
                connectionState = earlyConnection?.ConnectionState
                    ?? new ConnectionIoState(fd, conn.Session, _connectionIoStateLogger) { Pump = this };
                connectionState.SetHandshakeComplete();

                // Create DirectTlsConnection using fd directly (no Socket wrapper).
                // This avoids ~5+ syscalls per connection (fstat, getsockopt, fcntl, etc.)
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
                        _maxReadBufferSize,
                        _maxWriteBufferSize,
                        _directTlsConnectionLogger!,
                        negotiatedApplicationProtocol: conn.Session.NegotiatedApplicationProtocol,
                        clientCertificate: clientCertificate);  // Non-null only when the peer presented a client cert (mTLS)
                }
            }
            catch (Exception ex)
            {
                // Post-handshake activities failed (like cert validation). De-register fd here
                _logger.LogDebug(ex, "Completing handshake threw for fd={Fd}", fd);
                DropHandshake(fd, conn);
                return;
            }

            PromoteHandshakeToConnection(fd, connectionState);

            directConnection.Start();

            if (!_readyConnections.TryWrite(directConnection))
            {
                // Channel closed (shutting down) - dispose connection
                directConnection.DisposeAsync().AsTask().GetAwaiter().GetResult();
                _connectionTracker.ReleaseHandshake();
            }

            return;
        }

        if (status is TlsOperationStatus.NeedMoreData or TlsOperationStatus.DestinationTooSmall)
        {
            ApplyInProgressHandshakeInterest(fd, ref conn, status);
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
                _logger.LogDebug(ex, "Client certificate validation failed for fd={Fd}", fd);
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
                _logger.LogDebug("Handshake returned NeedsTlsContext but no certificate resolver is configured for fd={Fd}", fd);
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
                    _maxReadBufferSize,
                    _maxWriteBufferSize,
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
                _logger.LogDebug(ex, "SNI certificate resolution failed for fd={Fd}", fd);
                DropHandshake(fd, conn);
                return;
            }

            // Real context is now set; continue the handshake immediately.
            TryAdvanceHandshake(fd, conn);
            return;
        }

        // Handshake failed or connection closed - cleanup.
        _logger.LogDebug("Handshake failed for fd={Fd}: status={Status}", fd, status);
        DropHandshake(fd, conn);
    }

    // Adjusts an in-progress handshake's epoll interest set for a NeedMoreData / DestinationTooSmall step.
    // Established sockets are level-triggered, so EPOLLOUT must be armed only while there is pending handshake
    // output the socket send buffer could not accept (DestinationTooSmall), and cleared the moment the
    // handshake goes back to waiting on the peer (NeedMoreData). Leaving EPOLLOUT set while the send buffer
    // has room makes epoll_wait fire continuously and spins the pump at 100% CPU for the rest of the
    // handshake. Computes the desired mask from the status and reconciles against the cached
    // CurrentEpollInterest, so it only issues an epoll_ctl when the interest actually changes: a freshly
    // registered handshake is EPOLLIN-only, so a plain NeedMoreData (the common case) is a no-op here, and
    // repeated DestinationTooSmall steps re-use the already-armed interest. The caller only invokes this for
    // NeedMoreData / DestinationTooSmall, so any non-DestinationTooSmall status here means "back to EPOLLIN".
    // internal for testing.
    internal void ApplyInProgressHandshakeInterest(int fd, ref HandshakingConnection conn, TlsOperationStatus status)
    {
        uint desiredInterest = status switch
        {
            TlsOperationStatus.DestinationTooSmall => DefaultEpollInterest | NativeTls.EPOLLOUT,
            TlsOperationStatus.NeedMoreData => DefaultEpollInterest,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unexpected in-progress handshake status")
        };

        if (desiredInterest != conn.CurrentEpollInterest)
        {
            SetHandshakeInterest(fd, ref conn, desiredInterest);
        }
    }

    // Counterpart to DropHandshake: resets the fd to DefaultEpollInterest (dropping any handshake EPOLLOUT)
    // and moves it from _handshaking to _connections. Non-throwing, so the caller's start/enqueue tail is safe.
    private void PromoteHandshakeToConnection(int fd, ConnectionIoState connectionState)
    {
        var ev = new EpollEvent
        {
            Events = DefaultEpollInterest,
            Data = new EpollData { Fd = fd }
        };
        NativeTls.epoll_ctl(_epollFd, NativeTls.EPOLL_CTL_MOD, fd, ref ev);

        _connections[fd] = connectionState;
        _handshaking.Remove(fd);
    }

    // Tears down a handshake we will not surface to Kestrel - whether it failed (the handshake or the
    // completion path threw, or a client certificate was rejected) or completed after the transport had
    // already begun shutting down. Removes the fd from epoll and releases the session. If the ClientHello
    // listener already caused an early DirectTlsConnection to be allocated (at NeedsTlsContext), it is
    // released without the graceful TLS close_notify - a half-open session cannot shut down cleanly, so
    // AbortBeforeStart just completes the (never-started) pipes and closes the socket fd. Otherwise the
    // session is disposed directly, which closes the fd.
    private void DropHandshake(int fd, in HandshakingConnection conn)
    {
        _handshaking.Remove(fd);
        _connectionTracker.ReleaseHandshake();
        NativeTls.epoll_ctl(_epollFd, NativeTls.EPOLL_CTL_DEL, fd, IntPtr.Zero);
        ReleaseHandshakeResources(conn);
    }

    // Releases the native resources of a dropped handshake. Split out from DropHandshake (which owns the
    // pump-thread-local bookkeeping - dictionary removal and epoll de-registration) so tests can override
    // just the native teardown without a real TLS session or socket fd.
    private protected virtual void ReleaseHandshakeResources(in HandshakingConnection conn)
    {
        if (conn.Connection is { } earlyConnection)
        {
            earlyConnection.AbortBeforeStart();
        }
        else
        {
            conn.Session.Dispose();
        }
    }

    // Returns the Environment.TickCount64 deadline for a handshake starting at <paramref name="nowTimestamp"/>,
    // or long.MaxValue when handshake timeouts are disabled for this pump.
    internal long ComputeHandshakeDeadline(long nowTimestamp)
        => _handshakeTimeoutMs == long.MaxValue ? long.MaxValue : nowTimestamp + _handshakeTimeoutMs;

    // Drops every handshaking connection whose deadline has passed. Returns the number dropped this sweep.
    // Runs on the pump thread only. Connections with a long.MaxValue deadline (timeout disabled) are never
    // dropped, even if nowTimestamp is long.MaxValue.
    internal int SweepExpiredHandshakes(long nowTimestamp)
    {
        if (_handshaking.Count == 0)
        {
            return 0;
        }

        // Collect expired fds into a small stack buffer, then drop them in a second pass: a Dictionary
        // cannot be structurally modified (DropHandshake calls Remove) while it is being enumerated. The
        // buffer is bounded so a single sweep can never stall the pump under a flood of stalled handshakes;
        // any overflow beyond the buffer is caught by the next sweep (~10ms later), which is harmless
        // because those deadlines have already passed. 256 matches the epoll batch size (MaxEvents).
        Span<int> expired = stackalloc int[256];
        int count = 0;
        foreach (var kvp in _handshaking)
        {
            long deadline = kvp.Value.HandshakeDeadlineTimestamp;
            if (deadline != long.MaxValue && deadline <= nowTimestamp)
            {
                expired[count++] = kvp.Key;
                if (count == expired.Length)
                {
                    break;
                }
            }
        }

        for (int i = 0; i < count; i++)
        {
            int fd = expired[i];
            if (_handshaking.TryGetValue(fd, out var conn))
            {
                _logger.LogDebug("Handshake timed out for fd={Fd} after {TimeoutMs}ms; dropping connection.", fd, _handshakeTimeoutMs);
                DropHandshake(fd, conn);
            }
        }

        return count;
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
            _logger.LogDebug(ex, "Failed to read ClientHello length for fd={Fd}", connection.ConnectionState.Fd);
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
            _logger.LogDebug(ex, "TLS ClientHello listener callback threw for fd={Fd}", connection.ConnectionState.Fd);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Signals the pump loop to stop at its next iteration without waiting for the thread to exit. The owner
    /// signals every pump first, then awaits each (see <see cref="StopAndJoinAsync"/>), so the threads wind
    /// down concurrently and the total shutdown wait is bounded by the slowest thread rather than their sum.
    /// </summary>
    internal void SignalStop() => _running = false;

    /// <summary>
    /// Signals the pump to stop and asynchronously waits for its thread to actually exit, giving up when
    /// <paramref name="cancellationToken"/> is canceled. Unlike a blocking join, this does not park the caller's
    /// thread while it waits. Memoized, so repeated or concurrent calls observe a single shutdown.
    /// </summary>
    /// <param name="cancellationToken">Bounds the wait; on cancellation the method reports the thread as still running.</param>
    /// <returns>
    /// <see langword="true"/> if the pump thread has exited (or was never started), meaning it can no longer
    /// touch the epoll fd, the TLS contexts, or the memory pool, so the owner may safely release them.
    /// <see langword="false"/> if the thread is still running when the wait is canceled - for example stuck in a
    /// blocking user callback (certificate selector, certificate validation, or ClientHello listener). In that
    /// case the caller MUST NOT release any resource the pump can still reach, or it risks a use-after-free.
    /// </returns>
    public Task<bool> StopAndJoinAsync(CancellationToken cancellationToken)
    {
        // Memoize so a second (or concurrent) stop returns the same shutdown rather than re-running it - the
        // never-started branch closes the epoll fd exactly once, and the started branch must not re-await with
        // a fresh timeout.
        lock (_stopLock)
        {
            return _stopTask ??= StopAndJoinCoreAsync(cancellationToken);
        }
    }

    private async Task<bool> StopAndJoinCoreAsync(CancellationToken cancellationToken)
    {
        _running = false;

        // Non-owning wrapper: disposing it does not close the listener-owned fd. Safe even if the thread is
        // still alive - accept was already de-registered by StopAccepting, so the loop won't touch it.
        _listenSocket?.Dispose();

        if (!_threadStarted)
        {
            // The thread never ran, so PumpLoop's finally will never fire: close the epoll fd here instead.
            // Nothing else (contexts, pool) was ever handed to the loop, so this is all the cleanup needed.
            CloseEpollFd();
            return true;
        }

        try
        {
            // The pump thread completes _exitSignal only after it has released its handshakes and closed its
            // own epoll fd, so returning here proves it can no longer reach any owner-shared resource.
            await _exitSignal.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException)
        {
            // The wait was canceled - a user callback on the pump thread is blocking. It still owns its
            // epoll fd and may still reach the TLS contexts and the memory pool, so leave every resource intact
            // (the thread closes its own epoll fd if the callback ever returns; the listener leaks the
            // contexts/pool). The OS reclaims all of it at process exit; freeing it now would be a use-after-free.
            _logger.LogWarning("Pump {Id} thread did not exit (a TLS certificate, validation, or ClientHello callback may be blocking); deferring resource release to avoid a use-after-free.", _id);
            return false;
        }
    }

    // Closes the pump-owned epoll fd exactly once. Called by the pump thread in PumpLoop's finally for a started
    // pump, or by StopAndJoinCoreAsync for a never-started one - the Interlocked guard makes a stray double call
    // a no-op so it can never close an unrelated fd whose number was recycled.
    private void CloseEpollFd()
    {
        if (Interlocked.Exchange(ref _epollClosed, 1) != 0)
        {
            return;
        }

        // close() is intentionally not retried: on Linux the fd is released even when close returns EINTR, so a
        // retry could close an unrelated fd. A failure here (realistically only EBADF) signals a lifecycle bug
        // rather than a leak, so log it for diagnostics but don't act on it.
        if (NativeTls.close(_epollFd) < 0)
        {
            _logger.LogDebug("close(epollFd={EpollFd}) failed: errno={Errno}", _epollFd, Marshal.GetLastWin32Error());
        }
    }

    // Synchronous IDisposable bridge for callers (and tests) that use `using`. Blocking on the memoized stop
    // task cannot deadlock: _exitSignal uses RunContinuationsAsynchronously, so the continuation completes on a
    // thread pool thread, never inline on the thread calling Dispose.
    public void Dispose() => StopAndJoinAsync(CancellationToken.None).GetAwaiter().GetResult();
}
