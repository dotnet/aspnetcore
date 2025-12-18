// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Net.Sockets;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl.Ssl;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl.Workers;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl.Connection;

/// <summary>
/// Factory for creating <see cref="DirectSslConnection"/> instances.
/// </summary>
internal sealed class DirectSslConnectionContextFactory : IDisposable
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;
    private readonly MemoryPool<byte> _memoryPool;

    public DirectSslConnectionContextFactory(
        ILoggerFactory loggerFactory,
        MemoryPool<byte> memoryPool)
    {
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<DirectSslConnectionContextFactory>();
        _memoryPool = memoryPool;
    }

    public async ValueTask<DirectSslConnection?> CreateAsync(
        SslWorkerPool sslWorkerPool,
        Socket acceptSocket,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Creating DirectSslConnectionContext for {RemoteEndPoint}", acceptSocket.RemoteEndPoint);

        var handshakeRequest = await sslWorkerPool.SubmitHandshakeAsync(acceptSocket);
        if (handshakeRequest.Result != HandshakeResult.Success)
        {
            _logger.LogWarning("SSL handshake failed for {RemoteEndPoint}", acceptSocket.RemoteEndPoint);
            acceptSocket.Dispose();
            return null;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Connection cancelled after SSL handshake for {RemoteEndPoint}", acceptSocket.RemoteEndPoint);
            acceptSocket.Dispose();
            return null;
        }

        _logger.LogDebug("SSL handshake succeeded for {RemoteEndPoint}", acceptSocket.RemoteEndPoint);

        var connection = new DirectSslConnection(
            acceptSocket,
            handshakeRequest.Ssl,
            acceptSocket.LocalEndPoint,
            acceptSocket.RemoteEndPoint,
            _memoryPool,
            _loggerFactory.CreateLogger<DirectSslConnection>());

        connection.Start();
        return connection;
    }

    public void Dispose()
    {
        // MemoryPool is owned by caller
    }
}
