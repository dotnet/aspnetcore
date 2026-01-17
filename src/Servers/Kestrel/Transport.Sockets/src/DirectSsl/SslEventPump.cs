// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Uncomment the following line to enable debug counters for SSL diagnostics
// #define DIRECTSSL_DEBUG_COUNTERS

using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl.Interop;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl;

internal sealed class SslEventPump : IDisposable
{
    private readonly ILogger? _logger;
    private readonly int _id;

    private readonly int _epollFd;
    private readonly ConcurrentDictionary<int, SslConnectionState> _connections = new();
    private readonly Thread _pumpThread;
    private volatile bool _running = true;
    
#if DIRECTSSL_DEBUG_COUNTERS
    // Instance counters for this pump
    private long _totalRegistered;
    private long _totalUnregistered;
    private long _totalErrors;
    private long _totalRdhup;
    private long _totalRdhupWithData;
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
            Name = "SslEventPump",
            IsBackground = true
        };
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
            int numEvents = NativeSsl.epoll_wait(_epollFd, events, MaxEvents, timeout: 1000);
            
#if DIRECTSSL_DEBUG_COUNTERS
            // Log stats every 5 seconds
            var now = DateTime.UtcNow;
            if ((now - _lastLogTime).TotalSeconds >= 5)
            {
                _lastLogTime = now;
                Console.WriteLine($"[Pump {_id}] Connections: {_connections.Count}, Registered: {_totalRegistered}, Unregistered: {_totalUnregistered}, Errors: {_totalErrors}, RDHUP: {_totalRdhup}, RDHUP+Data: {_totalRdhupWithData}");
                Console.WriteLine($"[Pump {_id}] WriteEOF: {TotalWriteEof}, ReadEOF: {TotalReadEof}, WriteErr: {TotalWriteErrors}, ReadErr: {TotalReadErrors}");
                Console.WriteLine($"[Pump {_id}] SSL_ERR: Syscall={TotalSslErrorSyscall} (Ret0={TotalSslErrorSyscallRet0}, RetNeg1={TotalSslErrorSyscallRetNeg1})");
                Console.WriteLine($"[Pump {_id}] Errno: 0={TotalSslErrorSyscallErrno0}, EAGAIN={TotalSslErrorSyscallErrno11}, Other={TotalSslErrorSyscallErrnoOther}");
                Console.WriteLine($"[Pump {_id}] Write: Immediate={TotalWriteImmediate}, WouldBlock={TotalWriteWouldBlock}");
            }
#endif

            if (numEvents < 0)
            {
                int errno = Marshal.GetLastWin32Error();
                if (errno == 4)
                {
                    continue; // EINTR
                }

                break;
            }

            for (int i = 0; i < numEvents; i++)
            {
                _logger?.LogDebug("Processing event fd={Fd}, events={Events:X}", events[i].Data.Fd, events[i].Events);

                int fd = events[i].Data.Fd;
                uint mask = events[i].Events;

                if (fd == 0 && mask == 0)
                {
                    _logger?.LogDebug("Skipping spurious event with fd=0 and mask=0");
                    continue;
                }

                SslConnectionState? conn;
                _ = _connections.TryGetValue(fd, out conn);

                if (conn == null)
                {
                    continue;
                }

                if ((mask & (NativeSsl.EPOLLERR | NativeSsl.EPOLLHUP)) != 0)
                {
                    // Following nginx's approach: when error events occur, add EPOLLIN|EPOLLOUT
                    // to handle the events in at least one active handler. This allows pending
                    // reads/writes to complete before the error is surfaced.
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
    }

    public void Dispose()
    {
        _running = false;
        _pumpThread.Join(2000);
        NativeSsl.close(_epollFd);
    }
}
