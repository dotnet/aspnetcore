// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl.Interop;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl.Ssl;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl.Workers;

/// <summary>
/// A single SSL worker thread - nginx-style.
/// 
/// Runs a synchronous epoll loop that handles:
/// 1. TLS handshakes (non-blocking SSL_do_handshake)
/// 2. SSL reads (non-blocking SSL_read)
/// 3. SSL writes (non-blocking SSL_write)
/// 
/// All I/O for connections assigned to this worker happens on this single thread.
/// This eliminates thread context switching for high performance.
/// </summary>
internal sealed class SslWorker
{
    private readonly ILogger _logger;

    // Shared queue for new handshake requests (workers compete to dequeue)
    private readonly ConcurrentQueue<HandshakeRequest> _handshakeQueue;
    
    // Queue for I/O requests from this worker's connections (thread-safe for external submission)
    private readonly ConcurrentQueue<SslIoRequest> _ioQueue = new();

    // Active handshakes being processed by this worker
    private readonly Dictionary<int, HandshakeRequest> _activeHandshakes = [];
    
    // Active I/O operations waiting for socket readiness
    private readonly Dictionary<int, SslIoRequest> _pendingIo = [];

    private volatile bool _running;

    private readonly int _workerId;
    private readonly SslContext _sslContext;
    private readonly int _epollFd;
    private readonly Thread _thread;

    public int WorkerId => _workerId;
    public int EpollFd => _epollFd;

    public SslWorker(
        ILoggerFactory loggerFactory,
        int workerId,
        SslContext sslContext,
        ConcurrentQueue<HandshakeRequest> handshakeQueue)
    {
        _logger = loggerFactory.CreateLogger<SslWorker>();

        _workerId = workerId;
        _sslContext = sslContext;
        _handshakeQueue = handshakeQueue;

        // Create epoll instance for this worker
        _epollFd = NativeSsl.create_epoll();
        if (_epollFd < 0)
        {
            throw new InvalidOperationException($"Failed to create epoll for worker {workerId}");
        }

        _thread = new Thread(WorkerLoop)
        {
            Name = $"SslWorker-{workerId}",
            IsBackground = true
        };
    }

    public void Start()
    {
        _running = true;
        _thread.Start();
    }

    public void Stop()
    {
        _running = false;
        _thread.Join(timeout: TimeSpan.FromSeconds(2));
        NativeSsl.close_epoll(_epollFd);
    }

    /// <summary>
    /// Submit an SSL read request. Returns when data is available.
    /// </summary>
    public Task<int> SslReadAsync(IntPtr ssl, int clientFd, Memory<byte> buffer)
    {
        var request = new SslIoRequest(SslIoType.Read, ssl, clientFd, buffer, buffer.Length);
        _ioQueue.Enqueue(request);
        return request.Completion.Task;
    }

    /// <summary>
    /// Submit an SSL write request. Returns when all data is written.
    /// </summary>
    public Task<int> SslWriteAsync(IntPtr ssl, int clientFd, ReadOnlyMemory<byte> buffer, int length)
    {
        var request = new SslIoRequest(SslIoType.Write, ssl, clientFd, buffer, length);
        _ioQueue.Enqueue(request);
        return request.Completion.Task;
    }

    /// <summary>
    /// Main worker loop - runs synchronously on dedicated thread.
    /// nginx-style: single thread handles handshakes + all I/O for its connections.
    /// </summary>
    private void WorkerLoop()
    {
        _logger.LogInformation("Worker #{WorkerId} started, epoll_fd={EpollFd}", _workerId, _epollFd);

        while (_running)
        {
            // 1. Process new handshake requests from shared queue
            ProcessNewHandshakes();

            // 2. Process new I/O requests from connection contexts
            ProcessNewIoRequests();

            // 3. If nothing active AND no pending work in queues, sleep briefly
            if (_activeHandshakes.Count == 0 && _pendingIo.Count == 0 
                && _ioQueue.IsEmpty && _handshakeQueue.IsEmpty)
            {
                Thread.Sleep(1);
                continue;
            }

            // 4. If we have pending I/O or handshakes, wait for socket events
            if (_activeHandshakes.Count > 0 || _pendingIo.Count > 0)
            {
                var readyFd = NativeSsl.epoll_wait_one(_epollFd, 10);
                if (readyFd > 0)
                {
                    // 5. Handle the ready socket - could be handshake or I/O
                    ProcessReadySocket(readyFd);
                }
            }
        }

        CleanupOnShutdown();
        _logger.LogInformation("Worker #{WorkerId} stopped", _workerId);
    }

    #region Handshake Processing

    private void ProcessNewHandshakes()
    {
        while (_handshakeQueue.TryDequeue(out var request))
        {
            IntPtr ssl = NativeSsl.ssl_connection_create(
                ssl_ctx: _sslContext.Handle,
                client_fd: request.ClientFd,
                epoll_fd: _epollFd);

            if (ssl == IntPtr.Zero)
            {
                request.Result = HandshakeResult.ConnectionCreationFailed;
                request.Completion.TrySetResult(request);
                continue;
            }

            request.Ssl = ssl;
            request.Worker = this;
            _activeHandshakes[request.ClientFd] = request;

            // Try handshake immediately
            TryAdvanceHandshake(request);
        }
    }

    private void TryAdvanceHandshake(HandshakeRequest request)
    {
        var status = NativeSsl.ssl_try_handshake(request.Ssl, request.ClientFd, _epollFd);

        switch (status)
        {
            case NativeSsl.HANDSHAKE_COMPLETE:
                _activeHandshakes.Remove(request.ClientFd);
                request.Result = HandshakeResult.Success;
                request.Completion.TrySetResult(request);
                break;

            case NativeSsl.HANDSHAKE_WANT_READ:
            case NativeSsl.HANDSHAKE_WANT_WRITE:
                // Wait for next epoll event
                break;

            case NativeSsl.HANDSHAKE_ERROR:
            default:
                var errorMessage = NativeSsl.GetLastError();
                _logger.LogDebug("Handshake failed for fd={ClientFd}, status={Status}, error={Error}", 
                    request.ClientFd, status, errorMessage);
                _activeHandshakes.Remove(request.ClientFd);
                NativeSsl.ssl_connection_destroy(request.Ssl);
                request.Ssl = IntPtr.Zero;
                request.Result = HandshakeResult.Failed;
                request.Completion.TrySetResult(request);
                break;
        }
    }

    #endregion

    #region I/O Processing

    private void ProcessNewIoRequests()
    {
        while (_ioQueue.TryDequeue(out var request))
        {
            // Try the I/O operation immediately
            TryProcessIo(request);
        }
    }

    private unsafe void TryProcessIo(SslIoRequest request)
    {
        if (request.Type == SslIoType.Read)
        {
            TryRead(request);
        }
        else
        {
            TryWrite(request);
        }
    }

    private unsafe void TryRead(SslIoRequest request)
    {
        fixed (byte* ptr = request.ReadBuffer.Span)
        {
            int bytesRead = NativeSsl.ssl_read(request.Ssl, ptr, request.Length);

            if (bytesRead > 0)
            {
                // Success - complete immediately
                _pendingIo.Remove(request.ClientFd);
                request.Completion.TrySetResult(bytesRead);
            }
            else if (bytesRead == 0)
            {
                // EOF
                _pendingIo.Remove(request.ClientFd);
                request.Completion.TrySetResult(0);
            }
            else if (bytesRead == -1)
            {
                // Would block - register with epoll and wait
                RegisterForRead(request);
            }
            else
            {
                // Error
                _pendingIo.Remove(request.ClientFd);
                request.Completion.TrySetException(new IOException($"SSL_read failed: {bytesRead}"));
            }
        }
    }

    private unsafe void TryWrite(SslIoRequest request)
    {
        var remaining = request.WriteBuffer.Slice(request.BytesTransferred);
        
        fixed (byte* ptr = remaining.Span)
        {
            int toWrite = request.Length - request.BytesTransferred;
            int bytesWritten = NativeSsl.ssl_write(request.Ssl, ptr, toWrite);

            if (bytesWritten > 0)
            {
                request.BytesTransferred += bytesWritten;
                
                if (request.BytesTransferred >= request.Length)
                {
                    // All data written - complete
                    _pendingIo.Remove(request.ClientFd);
                    request.Completion.TrySetResult(request.BytesTransferred);
                }
                else
                {
                    // Partial write - wait for more buffer space
                    RegisterForWrite(request);
                }
            }
            else if (bytesWritten == -1)
            {
                // Would block - register with epoll and wait
                RegisterForWrite(request);
            }
            else
            {
                // Error
                _pendingIo.Remove(request.ClientFd);
                request.Completion.TrySetException(new IOException($"SSL_write failed: {bytesWritten}"));
            }
        }
    }

    private void RegisterForRead(SslIoRequest request)
    {
        _pendingIo[request.ClientFd] = request;
        NativeSsl.RegisterForRead(_epollFd, request.ClientFd);
    }

    private void RegisterForWrite(SslIoRequest request)
    {
        _pendingIo[request.ClientFd] = request;
        NativeSsl.RegisterForWrite(_epollFd, request.ClientFd);
    }

    #endregion

    #region Event Processing

    private void ProcessReadySocket(int fd)
    {
        // Check if this is a handshake
        if (_activeHandshakes.TryGetValue(fd, out var handshakeRequest))
        {
            TryAdvanceHandshake(handshakeRequest);
            return;
        }

        // Check if this is pending I/O
        if (_pendingIo.TryGetValue(fd, out var ioRequest))
        {
            TryProcessIo(ioRequest);
            return;
        }

        // Unknown fd - shouldn't happen
        _logger.LogWarning("Worker #{WorkerId}: Unknown fd {Fd} from epoll", _workerId, fd);
    }

    #endregion

    #region Cleanup

    private void CleanupOnShutdown()
    {
        _logger.LogInformation("Worker #{WorkerId} cleaning up...", _workerId);

        // Complete pending handshakes with error
        foreach (var kvp in _activeHandshakes)
        {
            var request = kvp.Value;
            if (request.Ssl != IntPtr.Zero)
            {
                NativeSsl.ssl_connection_destroy(request.Ssl);
                request.Ssl = IntPtr.Zero;
            }
            request.Result = HandshakeResult.SslWorkerPoolClosed;
            request.Completion.TrySetResult(request);
        }
        _activeHandshakes.Clear();

        // Complete pending I/O with error
        foreach (var kvp in _pendingIo)
        {
            var request = kvp.Value;
            request.Completion.TrySetException(new OperationCanceledException("SSL worker stopped"));
        }
        _pendingIo.Clear();
    }

    #endregion
}

