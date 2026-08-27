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
using Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls.UserCallbacks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls;

/// <summary>
/// TLS event pump that handles accept, handshake, and I/O events on a dedicated thread.
/// Uses EPOLLEXCLUSIVE on the listen socket to distribute accept load across workers.
/// </summary>
internal partial class TlsEventPump : IDisposable
{
    private readonly ILogger _logger;
    private readonly int _id;

    // Maximum time a connection is allowed to spend handshaking before the pump drops it. Stored as
    // milliseconds for cheap comparison against Environment.TickCount64. long.MaxValue means "no timeout"
    // (Timeout.InfiniteTimeSpan / TimeSpan.MaxValue) and is never enforced.
    private readonly long _handshakeTimeoutMs;

    private readonly int _epollFd;

    // Maximum connections accepted per AcceptConnections() call (one listen-fd epoll wake).
    internal const int MaxAcceptsPerIteration = 64;

    // Idle epoll_wait timeout when nothing time-based is pending: any real I/O wakes the loop immediately, so
    // this is just the maximum sleep between otherwise-idle iterations.
    internal const int IdlePollTimeoutMs = 1000;
    // Fast epoll_wait timeout used only while a handshake-timeout sweep is pending, so a stalled handshake is
    // swept within ~this many ms of its deadline even when the peer sends nothing to wake the loop.
    internal const int HandshakeSweepPollTimeoutMs = 10;

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
    // Guards the one-time close of the fds this pump owns (epoll + wakeup), which happens either once the pump
    // thread and its dispatched user callbacks have finished (started pump) or in StopAndJoinAsync (never-started
    // pump) - never both. Interlocked so a double close can't hit an unrelated fd whose number was recycled.
    private int _epollClosed;
    // Memoizes StopAndJoinAsync so repeated/concurrent stop calls observe one shutdown, not a re-run.
    private readonly object _stopLock = new();
    private Task<bool>? _stopTask;

    // Listen socket (added with EPOLLEXCLUSIVE). Volatile: written by StopAccepting() (on the disposing
    // thread) and read by the pump thread in PumpLoop/AcceptConnections.
    private volatile int _listenFd = -1;

    // Cross-thread wakeup for handshakes suspended on user code. A thread pool thread that finished a user
    // callback enqueues its result on _completedCallbacks and writes to this eventfd, which is registered in
    // this pump's epoll set, so the pump wakes immediately and resumes the handshake on its own thread.
    private readonly int _wakeupFd;

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

    // Invoked once if the epoll loop hits an unrecoverable (non-EINTR) failure. Lets the listener escalate a
    // single dead pump into a listener-wide fatal error (fault Accept + request host shutdown) instead of
    // silently leaving this pump's established connections unserviced.
    private Action<Exception> _onFatalError = static _ => { };

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

    // Set by the pump thread as it leaves the loop. Not redundant with _outstandingUserCallbacks: shutdown is
    // also reconsidered every time a user callback reports back, which happens throughout normal operation, so
    // without this flag the first callback to complete on a healthy pump would observe zero in-flight callbacks
    // and close the epoll and wakeup fds underneath the still-running loop.
    private volatile bool _loopExited;
    private int _shutdownCompleted;

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
        /// <summary>
        /// Non-null while this handshake is suspended waiting on user code running on the thread pool. The fd is
        /// de-registered from epoll for that whole window, so the connection generates no pump work, and the
        /// instance doubles as the resume token: a completion whose work item is not reference-equal to this one
        /// (fd recycled, connection already torn down) is discarded instead of resuming a stale handshake.
        /// </summary>
        public HandshakeUserCallback? PendingUserCallback;
    }

    public TlsEventPump(ILogger tlsPumpLogger, int id, TimeSpan handshakeTimeout)
    {
        _id = id;
        _logger = tlsPumpLogger;
        _handshakeTimeoutMs = handshakeTimeout == Timeout.InfiniteTimeSpan || handshakeTimeout == TimeSpan.MaxValue
            ? long.MaxValue
            : (long)handshakeTimeout.TotalMilliseconds;

        _epollFd = NativeTls.epoll_create1(NativeTls.EPOLL_CLOEXEC);
        if (_epollFd < 0)
        {
            throw new InvalidOperationException($"epoll_create1 failed: {Marshal.GetLastWin32Error()}");
        }

        _wakeupFd = CreateWakeupFd();

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
        EndPoint listenEndPoint,
        TlsContext tlsContext,
        Func<ConnectionContext?, string?, (TlsContext Context, RemoteCertificateValidationCallback? ClientCertificateValidation)>? contextResolver,
        ChannelWriter<DirectTlsConnection> readyConnections,
        MemoryPool<byte> memoryPool,
        ILoggerFactory loggerFactory,
        bool noDelay,
        long maxReadBufferSize,
        long maxWriteBufferSize,
        Action<Exception> onFatalError,
        Action<ConnectionContext, ReadOnlySequence<byte>>? clientHelloCallback = null,
        ConnectionTracker? connectionTracker = null,
        bool serverCertificateSelectorConfigured = true)
    {
        _listenFd = listenFd;
        ArgumentNullException.ThrowIfNull(tlsContext);
        ArgumentNullException.ThrowIfNull(onFatalError);
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
        _onFatalError = onFatalError;
        _listenEndPoint = listenEndPoint;

        // Cache loggers for connection creation
        _connectionIoStateLogger = loggerFactory.CreateLogger<ConnectionIoState>();
        _directTlsConnectionLogger = loggerFactory.CreateLogger<DirectTlsConnection>();

        // Either of these makes context resolution run user code, so the handshake must leave the event loop before resolving.
        _contextResolverRunsUserCode = serverCertificateSelectorConfigured || clientHelloCallback is not null;

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

    // The single checked EPOLL_CTL_MOD choke point for a handshaking fd. internal virtual so tests observe the
    // interest masks without a live epoll instance. Returns false (after logging errno) when the kernel rejects
    // the change, so callers can drop the connection rather than (a) waiting on an EPOLLOUT event that was never
    // registered mid-handshake or (b) leaving writable interest armed on a socket being promoted to established.
    // Callers that also track a HandshakingConnection should go through SetHandshakeInterest so the cached
    // CurrentEpollInterest stays in sync with the kernel interest set.
    internal virtual bool TryModifyHandshakeInterest(int fd, uint events)
    {
        var ev = new EpollEvent
        {
            Events = events,
            Data = new EpollData { Fd = fd }
        };

        if (NativeTls.epoll_ctl(_epollFd, NativeTls.EPOLL_CTL_MOD, fd, ref ev) < 0)
        {
            _logger.LogWarning("epoll_ctl MOD failed for handshaking fd={Fd}: errno={Errno}", fd, Marshal.GetLastWin32Error());
            return false;
        }

        return true;
    }

    // The single choke point for changing a handshaking fd's epoll interest set: rewrites the kernel interest
    // (via the TryModifyHandshakeInterest seam) AND records the new mask on the handshaking entry so
    // CurrentEpollInterest always mirrors what the kernel is subscribed to. Every mid-handshake interest
    // change must go through here so the cached mask cannot drift. Returns false when the kernel rejected the
    // change; the cache is then left untouched and the caller is expected to drop the connection.
    private bool SetHandshakeInterest(int fd, ref HandshakingConnection conn, uint events)
    {
        if (!TryModifyHandshakeInterest(fd, events))
        {
            return false;
        }

        conn.CurrentEpollInterest = events;
        _handshaking[fd] = conn;
        return true;
    }

    /// <summary>
    /// Modify the epoll events for an established connection's file descriptor.
    /// Used to dynamically add EPOLLOUT when a write would block.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when the kernel accepted the change; <see langword="false"/> (after logging errno)
    /// when it rejected it, so the caller can drop the connection instead of leaving it wedged on an interest the
    /// kernel never applied - a blocked write waiting on an EPOLLOUT that was never armed, or a level-triggered
    /// spin on writable interest that could not be cleared.
    /// </returns>
    public virtual bool ModifyEvents(int fd, uint events)
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

        if (NativeTls.epoll_ctl(_epollFd, NativeTls.EPOLL_CTL_MOD, fd, ref ev) < 0)
        {
            _logger.LogWarning("epoll_ctl MOD failed for fd={Fd}: errno={Errno}", fd, Marshal.GetLastWin32Error());
            return false;
        }

        return true;
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
                    int timeout = ComputePollTimeoutMs(_handshaking.Count);
                    int numEvents = events.Wait(_epollFd, timeout);

                    if (numEvents < 0)
                    {
                        int errno = Marshal.GetLastWin32Error();
                        if (errno == 4)
                        {
                            // EINTR: epoll_wait was interrupted by a signal before any fd was ready. Harmless
                            _logger.LogDebug("Pump {Id}: epoll_wait interrupted by a signal (EINTR); retrying.", _id);
                            continue;
                        }

                        _logger.LogCritical("epoll_wait failed: errno={Errno}", errno);

                        // Unrecoverable failure for this pump: retrying would leave this pump's established connections permanently unserviced
                        // while the listen socket and the other pumps keep the listener looking healthy.
                        if (_running)
                        {
                            _onFatalError.Invoke(new InvalidOperationException($"The DirectTls event pump {_id} failed: epoll_wait returned errno={errno}."));
                        }

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

                        // Cross-thread wakeup: a user callback finished on the thread pool. Consume the eventfd
                        // counter here; the queue itself is drained once below, after the whole batch.
                        if (fd == _wakeupFd)
                        {
                            DrainWakeup();
                            continue;
                        }

                        // Check if this is a handshaking connection
                        if (_handshaking.TryGetValue(fd, out var handshakingConn))
                        {
                            // A handshake suspended on user code has its fd de-registered from epoll, but an
                            // event for it may already be sitting in this batch (it was suspended earlier in
                            // the same iteration). Ignore it: only the resume path may touch the session.
                            if (handshakingConn.PendingUserCallback is null)
                            {
                                TryAdvanceHandshake(fd, handshakingConn);
                            }

                            continue;
                        }

                        // Established connection - dispatch its I/O. Extracted so the failure/drop paths are testable.
                        HandleConnectionEvent(fd, mask);
                    }

                    // Resume handshakes whose user callback completed. Done after the event batch so a resumed
                    // handshake is driven with the freshest state, and unconditionally (not only on a wakeup
                    // event) so a result that raced the eventfd read is never left parked.
                    DrainCompletedUserCallbacks();

                    // Drop connections whose handshake has taken too long. While a finite handshake timeout is
                    // configured and any handshake is in flight the epoll_wait timeout above is short (see
                    // ComputePollTimeoutMs), so a stalled handshake (e.g. a slow-loris ClientHello) is swept
                    // within ~that interval of its deadline even when the connection sends nothing to wake the pump.
                    if (_handshakeTimeoutMs != long.MaxValue && _handshaking.Count > 0)
                    {
                        SweepExpiredHandshakes(Environment.TickCount64);
                    }
                }
                catch (UnreachableException ex)
                {
                    _logger.LogCritical(ex, "Pump {Id} reached an unreachable state in PumpLoop", _id);

                    if (_running)
                    {
                        _onFatalError.Invoke(ex);
                    }
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Pump {Id} encountered an exception in PumpLoop", _id);
                }
            }
        }
        finally
        {
            // The thread owns its own teardown: release half-open handshakes, then hand the remaining teardown
            // (closing the fds it created in the constructor and signalling exit) to CompletePumpShutdownIfDrained.
            // Ordering matters - _exitSignal is the proof StopAndJoinAsync waits on before the listener frees the
            // TLS contexts and memory pool, so it must only fire once this thread is done AND no user callback is
            // still running on the thread pool (that callback would otherwise keep running against freed
            // resources, and could write to a wakeup fd whose number the OS had already recycled). In a finally so
            // a stray escape (or a break above) still signals, otherwise the awaiter would hang until its timeout.
            ReleasePendingHandshakes();
            _loopExited = true;
            CompletePumpShutdownIfDrained();
        }
    }

    // Release every half-open handshake after the event loop stops. Use the same ownership-aware teardown as
    // ordinary handshake failures so a DirectTlsConnection allocated at NeedsTlsContext is aborted as well.
    internal void ReleasePendingHandshakes()
    {
        foreach (var kvp in _handshaking)
        {
            // A handshake parked on user code must not be torn down yet: its work item may still be running,
            // and the certificate and validation sender it was handed belong to this session. Hold it aside and
            // release it once every dispatched callback has reported back (see CompletePumpShutdownIfDrained),
            // at which point nothing else can observe it.
            if (kvp.Value.PendingUserCallback is not null)
            {
                _handshakesAwaitingCallback.Add(kvp.Value);
                continue;
            }

            ReleaseHandshakeResources(kvp.Value);
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

        // EPOLLERR/EPOLLHUP are terminal: the socket is errored or fully hung up and can make no further
        // progress. They are also level-triggered, so the event re-fires on every epoll_wait until the fd is
        // de-registered. Force a final read/write dispatch below so an active awaitable can observe the real
        // failure through the normal I/O path, then drop unconditionally regardless of what the handlers did.
        bool errorOrHangup = (mask & (NativeTls.EPOLLERR | NativeTls.EPOLLHUP)) != 0;
        if (errorOrHangup)
        {
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

        // Terminal error/hangup: drop unconditionally after the dispatch above. When the connection is idle
        // (no active read or write awaitable) both handlers are no-ops and never throw, so without this the
        // level-triggered EPOLLERR/EPOLLHUP would re-fire on every epoll_wait and tight-spin the pump at 100%
        // CPU. OnError is a no-op on any awaitable the dispatch already completed and drives owner disposal for
        // an idle one. This also subsumes the EPOLLRDHUP handling below (which the mask|=EPOLLIN above would
        // otherwise defeat), so return here.
        if (errorOrHangup)
        {
            DropEstablishedConnection(fd);
            conn.OnError(new IOException("Connection error or hangup (EPOLLERR/EPOLLHUP)"));
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
    /// Accept new connections from the listen fd via <c>accept4</c>.
    /// Drains the accept backlog until <c>EAGAIN</c>, and stops if the pump is shutting down
    /// (<see cref="_running"/> cleared) or the listen socket has been detached
    /// (<see cref="_listenFd"/> set to -1 by <see cref="StopAccepting"/>).
    /// </summary>
    /// <remarks>
    /// accept4 reports outcomes as errno return values, so the drain runs without exceptions: no managed
    /// <see cref="Socket"/> exists until after a successful accept, so there is no ObjectDisposedException to
    /// race on shutdown. On any accept error other than EAGAIN/EINTR we stop the drain and return to
    /// <c>epoll_wait</c> rather than looping: the listen socket is level-triggered, so if connections are still
    /// pending we are re-woken immediately, and once <see cref="StopAccepting"/> has de-registered the fd we are
    /// never woken for it again. This makes the loop spin-proof without a failure counter - a persistently
    /// failing accept cannot tight-loop because a closed listen fd is always de-registered before it is closed.
    /// Successful accepts loop up to <see cref="MaxAcceptsPerIteration"/> per call so backlog draining under
    /// load is unaffected while a sustained flood still yields the pump thread between batches.
    /// </remarks>
    internal void AcceptConnections()
    {
        int acceptedCount = 0;
        while (_running && _listenFd >= 0)
        {
            int accepted = AcceptOne();
            if (accepted < 0)
            {
                int errno = -accepted;
                if (errno is NativeTls.EAGAIN)
                {
                    // Backlog drained - nothing more to accept right now.
                    break;
                }
                if (errno is NativeTls.EINTR)
                {
                    // Interrupted by a signal before a connection was accepted - retry (the guard above still
                    // lets a concurrent shutdown break out). Interrupted attempts don't count against the batch cap.
                    continue;
                }

                // Rare accept failure: a per-connection error (e.g. peer reset before accept) or the listen fd
                // torn down mid-drain (EBADF once the listener closes it). Stop this drain and let epoll decide
                // if there is more to do.
                _logger.LogDebug("Accept failed: errno={Errno}", errno);
                break;
            }

            Socket socket = WrapAcceptedFd(accepted);
            try
            {
                ProcessAcceptedSocket(socket);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Processing an accepted socket threw; disposing it to release its fd.");
                socket.Dispose();
            }

            // Cap accepts per wake so a sustained connection flood can't pin this thread in the accept drain and
            // starve established-connection I/O or the handshake-timeout sweep. The listen fd is level-triggered,
            // so any remaining backlog re-wakes epoll_wait immediately - we yield between batches without dropping
            // pending connections. In the common case the backlog is smaller than the cap and we exit via EAGAIN.
            if (++acceptedCount >= MaxAcceptsPerIteration)
            {
                break;
            }
        }
    }

    /// <summary>
    /// Accept a single pending connection from the listen fd. Isolated as the sole native accept call so tests
    /// can script accept outcomes without a real listen socket. Returns the accepted fd (>= 0) on success, or
    /// the negated errno (&lt; 0) on failure - accept4 reports EAGAIN/EBADF/EINTR as return values rather than
    /// exceptions.
    /// </summary>
    internal virtual int AcceptOne()
    {
        int fd = NativeTls.accept4(_listenFd, IntPtr.Zero, IntPtr.Zero, NativeTls.SOCK_NONBLOCK | NativeTls.SOCK_CLOEXEC);
        return fd >= 0 ? fd : -Marshal.GetLastWin32Error();
    }

    /// <summary>
    /// Wrap a freshly accepted fd in a managed <see cref="Socket"/> so its <see cref="Socket.RemoteEndPoint"/>
    /// and TCP_NODELAY option can be read/set without hand-rolling sockaddr parsing or a setsockopt P/Invoke.
    /// The wrapper owns the fd; ownership is transferred to the TLS session in
    /// <see cref="ProcessAcceptedSocket"/> before it is disposed. Isolated as a seam so accept-loop tests can
    /// inject a fake without a real fd.
    /// </summary>
    internal virtual Socket WrapAcceptedFd(int fd)
        => new Socket(new SafeSocketHandle((IntPtr)fd, ownsHandle: true));

    /// <summary>
    /// Configure a freshly accepted socket, create its TLS session, and register it for handshake
    /// events. Isolated from the accept loop so tests can exercise the loop's control flow without the
    /// native TLS/epoll work.
    /// </summary>
    internal virtual void ProcessAcceptedSocket(Socket accepted)
    {
        // Match Kestrel's MaxConcurrentConnections: accept, but if over limit reject the connection
        if (!_connectionTracker.TryAcquireHandshake())
        {
            _logger.LogDebug("Rejecting connection: in-flight connection cap reached");
            accepted.Dispose();
            return;
        }

        // The accepted fd is already non-blocking (accept4 was called with SOCK_NONBLOCK), so the session can
        // drive readiness via epoll. Only TCP_NODELAY remains to configure for low latency.
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
            _connectionTracker.ReleaseHandshake();
            return;
        }

        // Register client socket with epoll for handshake events
        if (!TryArmHandshakeInterest(clientFd, DefaultEpollInterest))
        {
            session.Dispose();
            _connectionTracker.ReleaseHandshake();
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
            // Mutual TLS (client certificate) handling. The endpoint opts in via
            // HttpsConnectionAdapterOptions.ClientCertificateMode (Allow/Require), which makes
            // CreateStreamTransportOptions set ClientCertificateRequired and install a
            // RemoteCertificateValidationCallback; conn.ClientCertificateValidation carries that callback
            // (null for server-auth-only endpoints, which skip this block entirely).
            //
            // The certificates are read from the session here (pump thread only), but the chain build and the
            // endpoint's callback are user-controlled work, so they are suspended onto the thread pool and the
            // handshake resumes in ResumeSuspendedHandshake.
            if (conn.ClientCertificateValidation is { } validateClientCertificate)
            {
                // The peer's leaf certificate, or null when the client presented none. On the fd fast path
                // this is the runtime's pending external-validation certificate. Intermediates are only
                // fetched when a leaf is present (they feed the chain's ExtraStore).
                var presentedCertificate = conn.Session.GetRemoteCertificate();
                var intermediates = presentedCertificate is null ? null : conn.Session.GetRemoteCertificates();

                var validationCallback = new ValidateClientCertificateCallback(
                    this,
                    fd,
                    conn.Connection,
                    conn.Session,
                    presentedCertificate,
                    intermediates,
                    validateClientCertificate);

                SuspendHandshake(fd, ref conn, validationCallback);
                return;
            }

            CompleteHandshake(fd, conn, clientCertificate: null);
            return;
        }

        if (status is TlsOperationStatus.NeedMoreData or TlsOperationStatus.DestinationTooSmall)
        {
            if (!ApplyInProgressHandshakeInterest(fd, ref conn, status))
            {
                // The socket could not be re-armed (for example arming EPOLLOUT failed). Dropping now avoids
                // stalling the handshake until its timeout, waiting on an event that was never registered.
                DropHandshake(fd, conn);
            }
            return;
        }

        if (status == TlsOperationStatus.NeedsCertificateValidation)
        {
            // The Linux fd fast handshake path reports Complete directly - it does not surface NeedsCertificateValidation like
            // the buffered PALs do, OpenSSL only enforces SSL_VERIFY_PEER (not FAIL_IF_NO_PEER_CERT), and the
            // fd read/write fast paths bypass the runtime's pending-validation fault.
            throw new UnreachableException($"The DirectTls handshake path reported {nameof(TlsOperationStatus.NeedsCertificateValidation)} for fd={fd}.");
        }

        if (status == TlsOperationStatus.NeedsTlsContext)
        {
            // Deferred SNI flow: the session parsed the ClientHello and needs the real per-host TLS context
            // before it can continue. Both the ClientHello listener and the certificate selector are user code
            // that can block for an unbounded time, so the pump copies everything they need off the session
            // here and then suspends the handshake: the fd leaves this pump's epoll set and the callbacks run
            // on the thread pool. ResumeSuspendedHandshake installs the resolved context and re-drives the
            // handshake back on the pump thread.
            if (_contextResolver is null)
            {
                // No selector configured but the session still deferred — misconfiguration.
                _logger.LogDebug("Handshake returned NeedsTlsContext but no certificate resolver is configured for fd={Fd}", fd);
                DropHandshake(fd, conn);
                return;
            }

            if (!_contextResolverRunsUserCode)
            {
                // Neither a certificate selector nor a ClientHello listener is configured, so resolution cannot
                // reach user code and there is nothing to move off the event loop. Resolve inline: this keeps
                // the fd armed and skips the suspension, the thread-pool hop and the early DirectTlsConnection
                // allocation the suspending path needs to give user code a stable ConnectionContext.
                ResolveTlsContextInline(fd, conn);
                return;
            }

            // Allocate the DirectTlsConnection now (its handshake is not yet complete) so both the
            // certificate selector and the optional ClientHello listener see the same
            // ConnectionContext / ConnectionId that will later serve the request; it is reused in the
            // Complete branch. The Connection-is-null guard makes this run exactly once even if the
            // handshake needs several more epoll round-trips. Because the bootstrap context carries no
            // credentials, every connection reaches NeedsTlsContext, so this early allocation is net-neutral
            // (moved from Complete, not added).
            bool firstSuspension = conn.Connection is null;
            if (firstSuspension && _memoryPool is not null)
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
            }

            // Copy the parsed ClientHello record out of the session while we are still on the pump thread; the
            // listener itself runs on the thread pool against this copy. Only on the first suspension, so a
            // handshake that needs several context round-trips still fires the listener exactly once.
            byte[]? clientHelloBuffer = null;
            int clientHelloLength = 0;
            if (firstSuspension && _clientHelloCallback is not null && conn.Connection is not null &&
                !TryCaptureClientHello(conn.Session, out clientHelloBuffer, out clientHelloLength))
            {
                _logger.LogDebug("Capturing the ClientHello record failed for fd={Fd}; dropping connection.", fd);
                DropHandshake(fd, conn);
                return;
            }

            var contextCallback = new ResolveTlsContextCallback(
                this,
                fd,
                conn.Connection,
                conn.Session.TargetHostName,
                _contextResolver,
                clientHelloBuffer is null ? null : _clientHelloCallback,
                clientHelloBuffer,
                clientHelloLength);

            SuspendHandshake(fd, ref conn, contextCallback);
            return;
        }

        // Handshake failed or connection closed - cleanup.
        _logger.LogDebug("Handshake failed for fd={Fd}: status={Status}", fd, status);
        DropHandshake(fd, conn);
    }

    // Resolves the TLS context on the pump thread and drives the handshake straight on. Only valid when the
    // resolver provably runs no user code (see _contextResolverRunsUserCode): it is the transport's own lambda
    // over a static certificate and a per-certificate TlsContext cache, so the only unbounded work is creating
    // the context on the first connection. Mirrors the ResolveTlsContextCallback arm of ResumeSuspendedHandshake,
    // minus the re-arm (the fd was never de-armed because the handshake never suspended).
    private void ResolveTlsContextInline(int fd, HandshakingConnection conn)
    {
        Debug.Assert(_contextResolver is not null, "ResolveTlsContextInline ran without a certificate resolver.");

        TlsContext context;
        RemoteCertificateValidationCallback? clientCertificateValidation;
        try
        {
            // conn.Connection is null here and stays null: with no selector the resolver ignores it, and with no
            // ClientHello listener nothing else needs a ConnectionContext this early. CompleteHandshake
            // allocates the DirectTlsConnection once the handshake is done.
            (context, clientCertificateValidation) = _contextResolver(conn.Connection, conn.Session.TargetHostName);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Resolving the TLS context failed for fd={Fd}; dropping connection.", fd);
            DropHandshake(fd, conn);
            return;
        }

        try
        {
            conn.Session.SetContext(context);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Installing the resolved TLS context failed for fd={Fd}", fd);
            DropHandshake(fd, conn);
            return;
        }

        // Persist the validation callback that came back with the context so the Complete branch can drive mTLS
        // validation even if the handshake needs several more epoll round-trips (each re-reads _handshaking[fd]).
        conn.ClientCertificateValidation = clientCertificateValidation;
        _handshaking[fd] = conn;

        TryAdvanceHandshake(fd, conn);
    }

    // Completes a handshake that has passed every user-code gate: builds (or promotes) the DirectTlsConnection,
    // moves the fd from handshaking to established, and hands the connection to Kestrel. Split out of
    // TryAdvanceHandshake so the client-certificate resume path can reach it directly without re-driving the
    // native handshake. Runs on the pump thread only.
    private void CompleteHandshake(int fd, HandshakingConnection conn, X509Certificate2? clientCertificate)
    {
        var earlyConnection = conn.Connection;
        ConnectionIoState connectionState;
        DirectTlsConnection directConnection;

        try
        {
            // Both are set before the pump thread starts and never cleared, so this is unreachable.
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
            // Post-handshake activities failed. De-register fd here
            _logger.LogDebug(ex, "Completing handshake threw for fd={Fd}", fd);
            DropHandshake(fd, conn);
            return;
        }

        if (!PromoteHandshakeToConnection(fd, connectionState))
        {
            // The socket could not be re-armed to the established interest set, so don't surface a
            // connection whose epoll interest is wrong (it would spin the pump on a stuck EPOLLOUT). It was
            // built but never Started and the fd is still registered as handshaking, so tear it down on
            // that path.
            DropCompletedHandshake(fd, directConnection);
            return;
        }

        directConnection.Start();

        if (!_readyConnections.TryWrite(directConnection))
        {
            // Channel closed (shutting down) - dispose connection
            _connectionTracker.ReleaseHandshake();
            _ = DisposeAbandonedConnectionAsync(directConnection);
        }
    }

    // Registers a handshaking fd in this pump's epoll set (EPOLL_CTL_ADD). Used when a connection is first
    // accepted and when a suspended handshake is resumed. internal virtual so tests can observe/reject the
    // registration without a live epoll instance.
    internal virtual bool TryArmHandshakeInterest(int fd, uint events)
    {
        var ev = new EpollEvent
        {
            Events = events,
            Data = new EpollData { Fd = fd }
        };

        if (NativeTls.epoll_ctl(_epollFd, NativeTls.EPOLL_CTL_ADD, fd, ref ev) < 0)
        {
            _logger.LogWarning("epoll_ctl ADD failed for handshaking fd={Fd}: errno={Errno}", fd, Marshal.GetLastWin32Error());
            return false;
        }

        return true;
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
    // Returns false when re-arming the interest set was rejected by the kernel so the caller drops the
    // connection instead of waiting on an event that was never registered. internal for testing.
    internal bool ApplyInProgressHandshakeInterest(int fd, ref HandshakingConnection conn, TlsOperationStatus status)
    {
        uint desiredInterest = status switch
        {
            TlsOperationStatus.DestinationTooSmall => DefaultEpollInterest | NativeTls.EPOLLOUT,
            TlsOperationStatus.NeedMoreData => DefaultEpollInterest,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unexpected in-progress handshake status")
        };

        if (desiredInterest != conn.CurrentEpollInterest)
        {
            return SetHandshakeInterest(fd, ref conn, desiredInterest);
        }

        return true;
    }

    // Counterpart to DropHandshake: resets the fd to DefaultEpollInterest (dropping any handshake EPOLLOUT)
    // and moves it from _handshaking to _connections. Returns false (without touching either dictionary) when
    // the kernel rejects the interest change, so the caller drops the built-but-not-yet-started connection
    // rather than surface one whose socket keeps writable interest armed and spins the pump.
    private bool PromoteHandshakeToConnection(int fd, ConnectionIoState connectionState)
    {
        if (!TryModifyHandshakeInterest(fd, DefaultEpollInterest))
        {
            return false;
        }

        _connections[fd] = connectionState;
        _handshaking.Remove(fd);
        return true;
    }

    // Teardown for a handshake that completed but could not be promoted to an established connection (the
    // socket could not be re-armed to the established interest set). Mirrors DropHandshake's pump-thread
    // bookkeeping, but the DirectTlsConnection was already built, so it is aborted directly - AbortBeforeStart
    // idempotently completes its idle pipes and closes the fd (via the session) for both the early-allocated
    // and default connection paths - instead of releasing the raw handshake session a second time.
    private void DropCompletedHandshake(int fd, DirectTlsConnection connection)
    {
        _handshaking.Remove(fd);
        _connectionTracker.ReleaseHandshake();
        NativeTls.epoll_ctl(_epollFd, NativeTls.EPOLL_CTL_DEL, fd, IntPtr.Zero);
        connection.AbortBeforeStart();
    }

    // Disposes the abandoned connection itself. internal virtual so tests can hold the disposal open and observe
    // that shutdown waits for it, without needing a live TLS session behind the connection.
    internal virtual ValueTask DisposeConnectionAsync(DirectTlsConnection connection) => connection.DisposeAsync();

    // Disposes a connection that finished its handshake just as the listener stopped accepting. It never reached
    // the ready channel, so nothing else will ever dispose it - and DisposeAsync is not quick: it awaits the
    // send/receive loops (which hold pooled buffers) and then sends close_notify through the TLS session, so the
    // work keeps touching the memory pool and the OpenSSL contexts after this method has yielded to its caller.
    // The listener frees both as soon as this pump reports that it has exited, so the disposal has to be part of
    // that report. The count is taken in the synchronous part of the method, which runs before the first await:
    // keeping it here rather than at the call site means a future caller cannot start a disposal without it
    // being counted. The returned task is deliberately not retained; the counter is what shutdown waits on.
    // internal so tests can drive this path without completing a real handshake.
    internal async Task DisposeAbandonedConnectionAsync(DirectTlsConnection connection)
    {
        Interlocked.Increment(ref _outstandingConnectionDisposals);

        try
        {
            await DisposeConnectionAsync(connection).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Disposing a connection abandoned during shutdown threw.");
        }
        finally
        {
            Interlocked.Decrement(ref _outstandingConnectionDisposals);
            CompletePumpShutdownIfDrained();
        }
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

    // Chooses the PumpLoop epoll_wait timeout. The fast poll is only worth paying when the sweep can actually
    // run - i.e. a finite handshake timeout is configured AND at least one handshake is in flight
    internal int ComputePollTimeoutMs(int handshakingCount)
        => _handshakeTimeoutMs != long.MaxValue && handshakingCount > 0 ? HandshakeSweepPollTimeoutMs : IdlePollTimeoutMs;

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
            // A handshake parked on user code is not stalled on the peer, and its work item may still be
            // running on the thread pool - dropping it here would release the connection underneath live user
            // code. It is swept on a later pass once it has resumed (its deadline is not extended, so an
            // over-long callback still costs the connection its handshake budget).
            if (kvp.Value.PendingUserCallback is not null)
            {
                continue;
            }

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

    // Copies the raw parsed ClientHello record out of the session so the UseTlsClientHelloListener callback can
    // run off the pump thread. Must be called on the pump thread (it touches the session); the copy is then
    // handed to a HandshakeUserCallback, which invokes the listener on the thread pool and returns the buffer
    // to the pool afterwards - so the callback still only sees a transient buffer, matching the
    // socket-transport TlsListener contract.
    //
    // Returns true when the handshake should continue - including when the session simply has no ClientHello
    // bytes to hand over, a non-exceptional empty result, in which case <paramref name="buffer"/> is null.
    // Returns false when the session could not produce the record, so the caller drops the connection (the
    // socket-transport TlsListener also fails the connection rather than swallowing this).
    private static bool TryCaptureClientHello(TlsSocketSession session, out byte[]? buffer, out int length)
    {
        buffer = null;
        length = 0;

        // Tracked outside the try so the catch returns the array even when the throw happened between renting it
        // and publishing it to buffer.
        byte[]? rented = null;
        try
        {
            var helloLength = session.GetClientHelloLength();
            if (helloLength <= 0)
            {
                return true;
            }

            rented = ArrayPool<byte>.Shared.Rent(helloLength);
            if (!session.TryGetClientHelloBytes(rented.AsSpan(0, helloLength), out var written) || written <= 0)
            {
                // The session reported a record but then could not hand it over, so the listener would silently
                // miss a ClientHello it was configured to see. Treat it as a capture failure, not as "no bytes".
                ArrayPool<byte>.Shared.Return(rented);
                return false;
            }

            buffer = rented;
            length = written;
            return true;
        }
        catch
        {
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }

            buffer = null;
            length = 0;
            return false;
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
    /// <see langword="true"/> if the pump thread has exited (or was never started) and no user callback it
    /// dispatched is still running, meaning nothing can touch the epoll fd, the TLS contexts, or the memory
    /// pool any more, so the owner may safely release them.
    /// <see langword="false"/> if the thread is still running, or a user callback (certificate selector,
    /// certificate validation, or ClientHello listener) it queued to the thread pool is still blocked, when the
    /// wait is canceled. In that case the caller MUST NOT release any resource the pump can still reach, or it
    /// risks a use-after-free.
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

        if (!_threadStarted)
        {
            // The thread never ran, so PumpLoop's finally will never fire: close the pump's fds here instead.
            // Nothing else (contexts, pool) was ever handed to the loop, and no user callback can be in flight,
            // so this is all the cleanup needed.
            CloseOwnedFds();
            return true;
        }

        try
        {
            // The pump thread completes _exitSignal only after it has released its handshakes, every user
            // callback it dispatched has reported back, and its fds are closed - so returning here proves
            // nothing can reach an owner-shared resource any more.
            await _exitSignal.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException)
        {
            // The wait was canceled - the pump thread, or a user callback it dispatched to the thread pool, is
            // still running. It may still reach the TLS contexts and the memory pool, so leave every resource
            // intact (the fds are closed once everything has finished; the listener leaks the contexts/pool). The OS
            // reclaims all of it at process exit; freeing it now would be a use-after-free.
            _logger.LogWarning("Pump {Id} did not finish (a TLS certificate, validation, or ClientHello callback may be blocking); deferring resource release to avoid a use-after-free.", _id);
            return false;
        }
    }

    // Closes the epoll and wakeup fds this pump created in its constructor, exactly once. Reached from the
    // drained-shutdown path for a started pump, or from StopAndJoinCoreAsync for a never-started one - the
    // Interlocked guard makes a stray double call a no-op so it can never close an unrelated fd whose number
    // was recycled.
    private void CloseOwnedFds()
    {
        if (Interlocked.Exchange(ref _epollClosed, 1) != 0)
        {
            return;
        }

        // close() is intentionally not retried: on Linux the fd is released even when close returns EINTR, so a
        // retry could close an unrelated fd. A failure here (realistically only EBADF) signals a lifecycle bug
        // rather than a leak, so log it for diagnostics but don't act on it.
        if (NativeTls.close(_wakeupFd) < 0)
        {
            _logger.LogDebug("close(wakeupFd={WakeupFd}) failed: errno={Errno}", _wakeupFd, Marshal.GetLastWin32Error());
        }

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
