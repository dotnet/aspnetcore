// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Uncomment the following line to enable debug counters for SSL diagnostics
// #define DIRECTSSL_DEBUG_COUNTERS

using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl.Connection;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl.Interop;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl;

/// <summary>
/// SSL event pump that handles accept, handshake, and I/O events on a dedicated thread.
/// Uses EPOLLEXCLUSIVE on the listen socket to distribute accept load across workers.
/// </summary>
internal sealed class SslEventPump : IDisposable
{
    private readonly ILogger? _logger;
    private readonly int _id;

    private readonly int _epollFd;

    // Established connections (handshake complete) - use fd as key
    private readonly ConcurrentDictionary<int, SslConnectionState> _connections = new();

    // Connections still handshaking - local to pump thread, no sync needed
    private readonly Dictionary<int, HandshakingConnection> _handshaking = new();

    private readonly Thread _pumpThread;
    private volatile bool _running = true;

    // Listen socket (added with EPOLLEXCLUSIVE)
    private int _listenFd = -1;
    private IntPtr _sslCtx = IntPtr.Zero;
    private ChannelWriter<DirectSslConnection>? _readyConnections;
    private MemoryPool<byte>? _memoryPool;
    private ILoggerFactory? _loggerFactory;
    private bool _noDelay;

    // Cached loggers for connection creation (initialized in StartWithListenSocket)
    private ILogger<SslConnectionState>? _sslConnectionStateLogger;
    private ILogger<DirectSslConnection>? _directSslConnectionLogger;

    // Cached listen endpoint to avoid getsockname syscall per connection
    private EndPoint? _listenEndPoint;

#if DIRECTSSL_DEBUG_COUNTERS
    // Instance counters for this pump
    private long _totalRegistered;
    private long _totalUnregistered;
    private long _totalErrors;
    private long _totalRdhup;
    private long _totalRdhupWithData;
    private long _totalAccepted;
    private long _totalHandshakeComplete;
    private long _totalHandshakeFailed;
    private DateTime _lastLogTime = DateTime.UtcNow;

    // Static counters that can be incremented from connection state
    public static long TotalWriteEof;
    public static long TotalReadEof;
    public static long TotalWriteErrors;
    public static long TotalReadErrors;
    public static long TotalSslErrorSyscall;
    public static long TotalSslErrorSyscallImmediate;  // SYSCALL on initial ReadAsync call
    public static long TotalSslErrorSyscallAfterEpoll; // SYSCALL after TryCompleteRead
    public static long TotalSslErrorSyscallRet0;       // SSL_read returned 0 (unexpected EOF)
    public static long TotalSslErrorSyscallRetNeg1;    // SSL_read returned -1 (syscall error)
    public static long TotalSslErrorSyscallErrno0;     // errno was 0
    public static long TotalSslErrorSyscallErrno11;    // errno was EAGAIN (11)
    public static long TotalSslErrorSyscallErrnoOther; // errno was something else
    public static long TotalSslErrorZeroReturn;
    public static long TotalSslErrorSsl;
    public static long TotalSslErrorOther;
    public static long TotalWriteWouldBlock;
    public static long TotalWriteImmediate;
    public static long TotalRequestsCompleted;  // Track completed request/response cycles
#endif

    /// <summary>
    /// Lightweight struct to track SSL connections during handshake.
    /// Uses less memory than SslConnectionState since we don't need full read/write machinery.
    /// NOTE: We don't create the Socket wrapper - use fd directly to avoid syscall overhead.
    /// </summary>
    private struct HandshakingConnection
    {
        public int Fd;
        public IntPtr Ssl;
        public System.Net.IPEndPoint? RemoteEndPoint;  // Captured from accept4 to avoid getpeername syscall
    }

    public SslEventPump(ILogger? sslPumpLogger, int id)
    {
        _id = id;
        _logger = sslPumpLogger;

        _epollFd = NativeSsl.epoll_create1(0);
        if (_epollFd < 0)
        {
            throw new InvalidOperationException($"epoll_create1 failed: {Marshal.GetLastWin32Error()}");
        }

        _pumpThread = new Thread(PumpLoop)
        {
            Name = $"SslEventPump-{id}",
            IsBackground = true
        };
    }

    /// <summary>
    /// Start the pump with a listen socket. The listen socket is registered with EPOLLEXCLUSIVE
    /// so that only one worker wakes per incoming connection (prevents thundering herd).
    /// </summary>
    public void StartWithListenSocket(
        int listenFd,
        IntPtr sslCtx,
        ChannelWriter<DirectSslConnection> readyConnections,
        MemoryPool<byte> memoryPool,
        ILoggerFactory loggerFactory,
        bool noDelay)
    {
        _listenFd = listenFd;
        _sslCtx = sslCtx;
        _readyConnections = readyConnections;
        _memoryPool = memoryPool;
        _loggerFactory = loggerFactory;
        _noDelay = noDelay;

        // Cache loggers for connection creation
        _sslConnectionStateLogger = loggerFactory.CreateLogger<SslConnectionState>();
        _directSslConnectionLogger = loggerFactory.CreateLogger<DirectSslConnection>();

        // Cache listen endpoint once to avoid getsockname syscall per connection
        // We need a temporary Socket wrapper to get the endpoint (this is a one-time cost)
        using (var tempSocket = new Socket(new SafeSocketHandle((IntPtr)listenFd, ownsHandle: false)))
        {
            _listenEndPoint = tempSocket.LocalEndPoint;
        }

        // Add listen socket with EPOLLEXCLUSIVE - only one worker wakes per connection
        var ev = new EpollEvent
        {
            Events = NativeSsl.EPOLLIN | NativeSsl.EPOLLEXCLUSIVE,
            Data = new EpollData { Fd = listenFd }
        };

        int result = NativeSsl.epoll_ctl(_epollFd, NativeSsl.EPOLL_CTL_ADD, listenFd, ref ev);
        if (result < 0)
        {
            int errno = Marshal.GetLastWin32Error();
            throw new InvalidOperationException($"Failed to add listen socket to epoll: errno={errno}");
        }

        _logger?.LogDebug("Pump {Id}: Added listen socket fd={Fd} with EPOLLEXCLUSIVE", _id, listenFd);

        // Start the pump thread
        _pumpThread.Start();
    }

    /// <summary>
    /// Start the pump without a listen socket.
    /// </summary>
    public void Start()
    {
        _pumpThread.Start();
    }

    public void Register(SslConnectionState conn)
    {
        _logger?.LogDebug("Registering fd={Fd} with epoll", conn.Fd);

        conn.Pump = this;
        _connections[conn.Fd] = conn;
#if DIRECTSSL_DEBUG_COUNTERS
        Interlocked.Increment(ref _totalRegistered);
#endif

        // Register for EPOLLIN initially - EPOLLOUT will be added dynamically when needed
        // Using level-triggered mode (no EPOLLET) for stability
        var ev = new EpollEvent
        {
            Events = NativeSsl.EPOLLIN | NativeSsl.EPOLLRDHUP,
            Data = new EpollData { Fd = conn.Fd }
        };

        int result = NativeSsl.epoll_ctl(_epollFd, NativeSsl.EPOLL_CTL_ADD, conn.Fd, ref ev);
        if (result < 0)
        {
            int errno = Marshal.GetLastWin32Error();
            _logger?.LogError("epoll_ctl ADD failed for fd={Fd}: errno={Errno}", conn.Fd, errno);
            throw new InvalidOperationException($"epoll_ctl ADD failed: {errno}");
        }

        _logger?.LogDebug("Successfully registered fd={Fd} with epoll", conn.Fd);
    }

    public void Unregister(int fd)
    {
        var removed = _connections.TryRemove(fd, out _);
#if DIRECTSSL_DEBUG_COUNTERS
        if (removed)
        {
            Interlocked.Increment(ref _totalUnregistered);
        }
#endif

        NativeSsl.epoll_ctl(_epollFd, NativeSsl.EPOLL_CTL_DEL, fd, IntPtr.Zero);
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
            Events = events | NativeSsl.EPOLLRDHUP,
            Data = new EpollData { Fd = fd }
        };

        int result = NativeSsl.epoll_ctl(_epollFd, NativeSsl.EPOLL_CTL_MOD, fd, ref ev);
        if (result < 0)
        {
            int errno = Marshal.GetLastWin32Error();
            _logger?.LogWarning("epoll_ctl MOD failed for fd={Fd}: errno={Errno}", fd, errno);
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
            int numEvents = NativeSsl.epoll_wait(_epollFd, events, MaxEvents, timeout);

#if DIRECTSSL_DEBUG_COUNTERS
            // Log stats every 5 seconds
            var now = DateTime.UtcNow;
            if ((now - _lastLogTime).TotalSeconds >= 5)
            {
                _lastLogTime = now;
                Console.WriteLine($"[Pump {_id}] Connections: {_connections.Count}, Handshaking: {_handshaking.Count}, Accepted: {_totalAccepted}");
                Console.WriteLine($"[Pump {_id}] Handshake: Complete={_totalHandshakeComplete}, Failed={_totalHandshakeFailed}");
                Console.WriteLine($"[Pump {_id}] Registered: {_totalRegistered}, Unregistered: {_totalUnregistered}, Errors: {_totalErrors}");
            }
#endif

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

                if ((mask & (NativeSsl.EPOLLERR | NativeSsl.EPOLLHUP)) != 0)
                {
                    // When error events occur, add EPOLLIN|EPOLLOUT
                    // to handle the events in at least one active handler.
                    mask |= NativeSsl.EPOLLIN | NativeSsl.EPOLLOUT;
#if DIRECTSSL_DEBUG_COUNTERS
                    Interlocked.Increment(ref _totalErrors);
#endif
                }

                // Process EPOLLIN first - even if EPOLLRDHUP is set, there may be data to read
                if ((mask & NativeSsl.EPOLLIN) != 0)
                {
                    conn.OnReadable();
                }

                if ((mask & NativeSsl.EPOLLOUT) != 0)
                {
                    conn.OnWritable();
                }

                // Handle EPOLLRDHUP - peer closed their write side
                if ((mask & NativeSsl.EPOLLRDHUP) != 0)
                {
#if DIRECTSSL_DEBUG_COUNTERS
                    if ((mask & NativeSsl.EPOLLIN) != 0)
                    {
                        Interlocked.Increment(ref _totalRdhupWithData);
                    }
                    else
                    {
                        Interlocked.Increment(ref _totalRdhup);
                    }
#endif
                    if ((mask & NativeSsl.EPOLLIN) == 0)
                    {
                        // No data to read, peer closed - signal error
                        _connections.TryRemove(fd, out _);
#if DIRECTSSL_DEBUG_COUNTERS
                        Interlocked.Increment(ref _totalErrors);
#endif
                        conn.OnError(new IOException("Peer closed connection"));
                    }
                }
            }
        }

        // Cleanup handshaking connections
        foreach (var kvp in _handshaking)
        {
            var conn = kvp.Value;
            if (conn.Ssl != IntPtr.Zero)
            {
                NativeSsl.SSL_free(conn.Ssl);
            }
            NativeSsl.close(conn.Fd);
        }
        _handshaking.Clear();
    }

    /// <summary>
    /// Accept new connections from the listen socket.
    /// Loops until EAGAIN (no more pending connections).
    /// Captures peer address from accept4 to avoid getpeername syscall later.
    /// </summary>
    private void AcceptConnections()
    {
        while (true)
        {
            // Use accept4 with address capture to avoid separate getpeername syscall
            var (clientFd, remoteEndPoint) = NativeSsl.AcceptNonBlockingWithPeerAddress(_listenFd);

            if (clientFd == -1)
            {
                // EAGAIN - no more pending connections
                break;
            }

            if (clientFd == -2)
            {
                // Error - continue trying
                continue;
            }

#if DIRECTSSL_DEBUG_COUNTERS
            Interlocked.Increment(ref _totalAccepted);
#endif

            // Set TCP_NODELAY for low latency
            if (_noDelay)
            {
                NativeSsl.SetTcpNoDelay(clientFd);
            }

            // Create SSL and bind to socket
            IntPtr ssl = NativeSsl.SSL_new(_sslCtx);
            if (ssl == IntPtr.Zero)
            {
#if DIRECTSSL_DEBUG_COUNTERS
                Interlocked.Increment(ref _totalHandshakeFailed);
#endif
                NativeSsl.close(clientFd);
                continue;
            }

            NativeSsl.SSL_set_fd(ssl, clientFd);
            NativeSsl.SSL_set_accept_state(ssl);

            // Register client socket with epoll for handshake events
            var ev = new EpollEvent
            {
                Events = NativeSsl.EPOLLIN | NativeSsl.EPOLLRDHUP,
                Data = new EpollData { Fd = clientFd }
            };

            int result = NativeSsl.epoll_ctl(_epollFd, NativeSsl.EPOLL_CTL_ADD, clientFd, ref ev);
            if (result < 0)
            {
                int errno = Marshal.GetLastWin32Error();
                _logger?.LogWarning("epoll_ctl ADD failed for handshaking fd={Fd}: errno={Errno}", clientFd, errno);
#if DIRECTSSL_DEBUG_COUNTERS
                Interlocked.Increment(ref _totalHandshakeFailed);
#endif
                NativeSsl.SSL_free(ssl);
                NativeSsl.close(clientFd);
                continue;
            }

            // Track handshaking connection with captured remote endpoint
            _handshaking[clientFd] = new HandshakingConnection
            {
                Fd = clientFd,
                Ssl = ssl,
                RemoteEndPoint = remoteEndPoint
            };

            // Try handshake immediately (might complete for resumed sessions)
            TryAdvanceHandshake(clientFd, _handshaking[clientFd]);
        }
    }

    /// <summary>
    /// Try to advance the TLS handshake for a connection.
    /// </summary>
    private void TryAdvanceHandshake(
        int fd,
        HandshakingConnection conn)
    {
        NativeSsl.ERR_clear_error();
        int n = NativeSsl.SSL_do_handshake(conn.Ssl);

        if (n == 1)
        {
            // Handshake complete! Create connection and enqueue to Kestrel
#if DIRECTSSL_DEBUG_COUNTERS
            Interlocked.Increment(ref _totalHandshakeComplete);
#endif
            _handshaking.Remove(fd);

            // Create SslConnectionState for the established connection
            var connectionState = new SslConnectionState(fd, conn.Ssl, _sslConnectionStateLogger);
            connectionState.SetHandshakeComplete();

            // Register with our connections dictionary and epoll
            connectionState.Pump = this;
            _connections[fd] = connectionState;

            // Update epoll to use standard connection events (already registered, just confirm)
            var ev = new EpollEvent
            {
                Events = NativeSsl.EPOLLIN | NativeSsl.EPOLLRDHUP,
                Data = new EpollData { Fd = fd }
            };
            NativeSsl.epoll_ctl(_epollFd, NativeSsl.EPOLL_CTL_MOD, fd, ref ev);

            // Create DirectSslConnection using fd directly (no Socket wrapper)
            // This avoids ~5+ syscalls per connection (fstat, getsockopt, fcntl, etc.)
            if (_readyConnections != null && _memoryPool != null)
            {
                var directConnection = new DirectSslConnection(
                    fd,                           // Use fd directly - no Socket wrapper
                    connectionState,
                    this,
                    _listenEndPoint,              // Cached - avoids getsockname syscall
                    conn.RemoteEndPoint,          // Captured from accept4 - avoids getpeername syscall
                    _memoryPool,
                    _directSslConnectionLogger!);

                directConnection.Start();

                if (!_readyConnections.TryWrite(directConnection))
                {
                    // Channel closed (shutting down) - dispose connection
                    directConnection.DisposeAsync().AsTask().GetAwaiter().GetResult();
                }
            }
            return;
        }

        int error = NativeSsl.SSL_get_error(conn.Ssl, n);

        if (error == NativeSsl.SSL_ERROR_WANT_READ)
        {
            // Already registered for EPOLLIN, just wait
            return;
        }

        if (error == NativeSsl.SSL_ERROR_WANT_WRITE)
        {
            // Need to write - add EPOLLOUT
            var ev = new EpollEvent
            {
                Events = NativeSsl.EPOLLIN | NativeSsl.EPOLLOUT | NativeSsl.EPOLLRDHUP,
                Data = new EpollData { Fd = fd }
            };
            NativeSsl.epoll_ctl(_epollFd, NativeSsl.EPOLL_CTL_MOD, fd, ref ev);
            return;
        }

        // Handshake failed - cleanup
        _logger?.LogDebug("Handshake failed for fd={Fd}: error={Error}", fd, error);
#if DIRECTSSL_DEBUG_COUNTERS
        Interlocked.Increment(ref _totalHandshakeFailed);
#endif
        _handshaking.Remove(fd);
        NativeSsl.epoll_ctl(_epollFd, NativeSsl.EPOLL_CTL_DEL, fd, IntPtr.Zero);
        NativeSsl.SSL_free(conn.Ssl);
        NativeSsl.close(fd);
    }

    public void Dispose()
    {
        _running = false;
        _pumpThread.Join(2000);
        NativeSsl.close(_epollFd);
    }
}
