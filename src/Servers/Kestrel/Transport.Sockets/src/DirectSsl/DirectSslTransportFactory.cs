// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl.Ssl;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl.Workers;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl.Connection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl;

/// <summary>
/// A factory for direct-ssl based connections.
/// </summary>
internal sealed class DirectSslTransportFactory : IConnectionListenerFactory, IConnectionListenerFactorySelector
{
    private SslContext? _sslContext;
    private SslWorkerPool? _sslWorkerPool;

    private readonly DirectSslTransportOptions _options;

    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DirectSslTransportFactory"/> class.
    /// </summary>
    /// <param name="options">The transport options.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    public DirectSslTransportFactory(
        IOptions<DirectSslTransportOptions> options,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _options = options.Value;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<DirectSslTransportFactory>();
    }

    /// <inheritdoc />
    public ValueTask<IConnectionListener> BindAsync(EndPoint endpoint, CancellationToken cancellationToken = default)
    {
        // Initialize SSL context lazily from options
        if (_sslContext is null)
        {
            if (string.IsNullOrEmpty(_options.CertificatePath) || string.IsNullOrEmpty(_options.PrivateKeyPath))
            {
                throw new InvalidOperationException("CertificatePath and PrivateKeyPath must be configured in DirectSslTransportOptions.");
            }

            _sslContext = new SslContext(_options.CertificatePath, _options.PrivateKeyPath);
            _logger.LogInformation("SSL context initialized with certificate: {CertPath}", _options.CertificatePath);
        }

        // Initialize SSL worker pool lazily
        if (_sslWorkerPool is null)
        {
             _sslWorkerPool = new SslWorkerPool(_loggerFactory, _sslContext, _options.WorkerCount);
            _logger.LogInformation("SSL worker pool started with {WorkerCount} workers.", _options.WorkerCount);         
        }

        var transport = new DirectSslConnectionListener(_loggerFactory, _sslContext, _sslWorkerPool, endpoint, _options);
        transport.Bind();
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
