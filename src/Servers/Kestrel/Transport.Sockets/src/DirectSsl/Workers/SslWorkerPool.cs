// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Net.Sockets;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl.Ssl;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl.Workers;

/// <summary>
/// Pool of dedicated SSL worker threads.
/// Each worker has its own epoll instance and processes handshakes independently.
/// All workers share a common queue of incoming handshake requests.
/// </summary>
internal sealed class SslWorkerPool : IDisposable
{
    private readonly ILogger _logger;

    readonly SslWorker[] _workers;
    private readonly ConcurrentQueue<HandshakeRequest> _sharedQueue = new(); // Shared across all workers

    private readonly SslContext _sslContext;
    private bool _disposed;

    public SslWorkerPool(ILoggerFactory loggerFactory, SslContext sslContext, int workerCount)
    {
        _logger = loggerFactory.CreateLogger<SslWorkerPool>();

        _sslContext = sslContext;
        _workers = new SslWorker[workerCount];

        for (var i = 0; i < workerCount; i++)
        {
            _workers[i] = new SslWorker(loggerFactory, i, sslContext, _sharedQueue);
            _workers[i].Start();
        }

        _logger.LogInformation("Started {workerCount} workers with shared queue", workerCount);
    }

    /// <summary>
    /// Submit a socket for TLS handshake.
    /// Returns a task that completes when handshake is done.
    /// Any free worker will pick it up.
    /// </summary>
    public Task<HandshakeResult> SubmitHandshakeAsync(Socket clientSocket)
    {
        var request = new HandshakeRequest(clientSocket);
        _sharedQueue.Enqueue(request);
        
        return request.Completion.Task;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            foreach (var worker in _workers)
            {
                worker.Stop();
            }
            _disposed = true;
        }
    }
}