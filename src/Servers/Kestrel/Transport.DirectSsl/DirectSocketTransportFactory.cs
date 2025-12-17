// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.DirectSsl.Internal.OpenSSL;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectSsl;

/// <summary>
/// A factory for direct socket connections with integrated OpenSSL TLS handling.
/// This factory creates connections that bypass the traditional SslStream layer
/// and register sockets directly with OpenSSL for zero-copy TLS processing.
/// </summary>
public sealed class DirectSocketTransportFactory : IConnectionListenerFactory, IConnectionListenerFactorySelector, IDisposable
{
    private readonly DirectSocketTransportOptions _options;
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
        IOptions<DirectSocketTransportOptions> options,
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

    /// <summary>
    /// Initializes the SSL context with certificate and key files.
    /// </summary>
    /// <param name="certPath">Path to the certificate file (PEM format).</param>
    /// <param name="keyPath">Path to the private key file (PEM format).</param>
    public void InitializeSslContext(string certPath, string keyPath)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_sslContext is not null)
        {
            _logger.LogWarning("SSL context is already initialized. Dispose and create a new factory to change the certificate.");
            return;
        }

        try
        {
            _sslContext = new OpenSSLContext(certPath, keyPath);
            _logger.LogInformation("Direct SSL context initialized successfully with certificate from {CertPath}", certPath);
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

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            _sslContext?.Dispose();
            _disposed = true;
        }
    }
}
