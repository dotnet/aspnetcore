// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl.Ssl;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl;

/// <summary>
/// A factory for direct-ssl based connections.
/// </summary>
public sealed class DirectSslTransportFactory : IConnectionListenerFactory, IConnectionListenerFactorySelector
{
    private SslContext? _sslContext;

    private readonly SocketTransportOptions _options;
    
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SocketTransportFactory"/> class.
    /// </summary>
    /// <param name="options">The transport options.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    public DirectSslTransportFactory(
        IOptions<SocketTransportOptions> options,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _options = options.Value;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<DirectSslTransportFactory>();
    }

    /// <summary>
    /// Initializes the SSL context with the provided certificate and key paths.
    /// </summary>
    /// <param name="certPath"></param>
    /// <param name="keyPath"></param>
    public void InitializeSslContext(string certPath, string keyPath)
    {
        if (_sslContext is not null)
        {
            _logger.LogWarning("SSL context is already initialized. Dispose and create a new factory to change the certificate.");
            return;
        }

        try
        {
            _sslContext = new SslContext(certPath, keyPath);    
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize SSL context.");
            throw;
        }
    }

    /// <inheritdoc />
    public ValueTask<IConnectionListener> BindAsync(EndPoint endpoint, CancellationToken cancellationToken = default)
    {
        var transport = new DirectSslConnectionListener();

        return new ValueTask<IConnectionListener>(transport);
    }

    /// <inheritdoc />
    public bool CanBind(EndPoint endpoint) => endpoint switch
    {
        IPEndPoint _ => true,
        UnixDomainSocketEndPoint _ => true,
        FileHandleEndPoint _ => true,
        _ => false
    };
}
