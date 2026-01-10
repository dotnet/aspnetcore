// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
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

        // Only register for EPOLLIN initially - EPOLLOUT will be added dynamically when needed
        // This avoids spurious wakeups since sockets are almost always writable
        var ev = new EpollEvent
        {
            Events = NativeSsl.EPOLLIN | NativeSsl.EPOLLET | NativeSsl.EPOLLRDHUP,
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
        Debug.Assert(removed, "Unregister called for fd not in connections");

        NativeSsl.epoll_ctl(_epollFd, NativeSsl.EPOLL_CTL_DEL, fd, IntPtr.Zero);
    }

    /// <summary>
    /// Modify the epoll events for a file descriptor.
    /// Used to dynamically add EPOLLOUT when a write would block.
    /// </summary>
    public void ModifyEvents(int fd, uint events)
    {
        var ev = new EpollEvent
        {
            Events = events | NativeSsl.EPOLLET | NativeSsl.EPOLLRDHUP,
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
            _logger?.LogDebug("epoll_wait returned {NumEvents} events", numEvents);

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
                    conn.OnError(new IOException("Socket error or hangup"));
                    continue;
                }

                if ((mask & NativeSsl.EPOLLIN) != 0)
                {
                    conn.OnReadable();
                }

                if ((mask & NativeSsl.EPOLLOUT) != 0)
                {
                    conn.OnWritable();
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
