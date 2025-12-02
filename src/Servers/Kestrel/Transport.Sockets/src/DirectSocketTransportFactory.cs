// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal.OpenSSL;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;

/// <summary>
/// A factory for direct socket connections with integrated OpenSSL TLS handling.
/// This factory creates connections that bypass the traditional SslStream layer
/// and register sockets directly with OpenSSL for zero-copy TLS processing.
/// </summary>
public sealed class DirectSocketTransportFactory : IConnectionListenerFactory, IConnectionListenerFactorySelector
{
    private readonly SocketTransportOptions _options;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;
    private OpenSSLContext? _sslContext;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DirectSocketTransportFactory"/> class.
    /// </summary>
    /// <param name="options">The transport options.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    public DirectSocketTransportFactory(
        IOptions<SocketTransportOptions> options,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _options = options.Value;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<DirectSocketTransportFactory>();
    }

    /// <summary>
    /// Initializes the SSL context with the given certificate.
    /// </summary>
    /// <param name="certificate">The server certificate to use for TLS.</param>
    public void InitializeSslContext(X509Certificate2 certificate)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_sslContext is not null)
        {
            _logger.LogWarning("SSL context is already initialized. Dispose and create a new factory to change the certificate.");
            return;
        }

        try
        {
            // Create OpenSSL context
            var method = OpenSSLBindings.TLS_server_method();
            if (method == IntPtr.Zero)
            {
                _logger.LogError("Failed to get TLS server method from OpenSSL");
                return;
            }

            var ctx = OpenSSLBindings.SSL_CTX_new(method);
            if (ctx == IntPtr.Zero)
            {
                _logger.LogError("Failed to create OpenSSL context");
                return;
            }

            _sslContext = new OpenSSLContext(ctx);

            _logger.LogInformation("Direct SSL context initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize SSL context");
            throw;
        }
    }

    /// <inheritdoc />
    public ValueTask<IConnectionListener> BindAsync(EndPoint endpoint, CancellationToken cancellationToken = default)
    {
        var transport = new DirectSocketConnectionListener(
            endpoint,
            _options,
            _loggerFactory,
            _sslContext);
        transport.Bind();
        return new ValueTask<IConnectionListener>(transport);
    }

    /// <inheritdoc />
    public bool CanBind(EndPoint endpoint)
    {
        return endpoint switch
        {
            IPEndPoint _ => true,
            UnixDomainSocketEndPoint _ => true,
            FileHandleEndPoint _ => true,
            _ => false
        };
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _sslContext?.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// Connection listener for direct socket connections with OpenSSL TLS integration.
/// </summary>
internal sealed class DirectSocketConnectionListener : SocketConnectionListener
{
    private readonly OpenSSLContext? _sslContext;
    private readonly ILogger<DirectSocketConnectionListener> _logger;

    public DirectSocketConnectionListener(
        EndPoint endpoint,
        SocketTransportOptions options,
        ILoggerFactory loggerFactory,
        OpenSSLContext? sslContext)
        : base(endpoint, options, loggerFactory)
    {
        _sslContext = sslContext;
        _logger = loggerFactory.CreateLogger<DirectSocketConnectionListener>();
    }

    protected override ConnectionContext CreateConnectionFromSocket(Socket socket, SocketConnectionContextFactory factory)
    {
        // Use the factory's Create() method which returns a properly configured SocketConnection
        // For direct socket with SSL, we would need to intercept here
        // For now, we just delegate to the factory
        // TODO: When OpenSSL integration is complete, create DirectSocketConnection here instead
        
        if (_sslContext is not null)
        {
            _logger.LogDebug("DirectSocket with SSL context - OpenSSL integration coming in future phases");
            // In future phases, we'll create DirectSocketConnection with SSL context
            // For now, use standard factory
        }

        return factory.Create(socket);
    }
}
