// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl.Ssl;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl.Workers;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl.Connection;

/// <summary>
/// Factory for creating <see cref="DirectSslConnectionContext"/> instances.
/// </summary>
internal sealed class DirectSslConnectionContextFactory : IDisposable
{
    private readonly ILogger _logger;
    private readonly DirectSslConnectionContextFactoryOptions _options;

    public DirectSslConnectionContextFactory(
        ILoggerFactory loggerFactory,
        DirectSslTransportOptions transportOptions)
        : this(loggerFactory, new DirectSslConnectionContextFactoryOptions(transportOptions))
    {
    }

    public DirectSslConnectionContextFactory(
        ILoggerFactory loggerFactory,
        DirectSslConnectionContextFactoryOptions options)
    {
        _logger = loggerFactory.CreateLogger<DirectSslConnectionContextFactory>();
        _options = options;
    }

    public async ValueTask<DirectSslConnectionContext> CreateAsync(
        SslWorkerPool sslWorkerPool,
        Socket acceptSocket,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Creating DirectSslConnectionContext.");

        var handshakeResult = await sslWorkerPool.SubmitHandshakeAsync(acceptSocket);
        if (handshakeResult == HandshakeResult.Success)
        {
            _logger.LogDebug("SSL handshake succeeded.");
            return new DirectSslConnectionContext();
        }

        if (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug("SSL handshake canceled.");
            throw new OperationCanceledException(cancellationToken);
        }

        throw new NotImplementedException("failed ssl handshake!");
    }

    public void Dispose()
    {
    }
}
