// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl.Interop;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl;

internal sealed class SslEventPump : IDisposable
{
    private readonly ILogger? _logger;
    private readonly int _id;

    private readonly int _epollFd;
    private readonly Dictionary<int, SslConnectionState> _connections = new();
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
        lock (_connections)
        {
            _connections[conn.Fd] = conn;
        }

        var ev = new EpollEvent
        {
            Events = NativeSsl.EPOLLIN | NativeSsl.EPOLLOUT | NativeSsl.EPOLLET | NativeSsl.EPOLLRDHUP,
            Data = new EpollData { Fd = conn.Fd }
        };
        
        if (NativeSsl.epoll_ctl(_epollFd, NativeSsl.EPOLL_CTL_ADD, conn.Fd, ref ev) < 0)
        {
            throw new InvalidOperationException($"epoll_ctl ADD failed: {Marshal.GetLastWin32Error()}");
        }
    }

    public void Unregister(int fd)
    {
        lock (_connections)
        {
            _connections.Remove(fd);
        }
        
        NativeSsl.epoll_ctl(_epollFd, NativeSsl.EPOLL_CTL_DEL, fd, IntPtr.Zero);
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

                SslConnectionState? conn;
                lock (_connections)
                {
                    _connections.TryGetValue(fd, out conn);
                }

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
