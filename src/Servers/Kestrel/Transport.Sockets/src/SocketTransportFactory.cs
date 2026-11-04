// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;

/// <summary>
/// A factory for socket based connections.
/// </summary>
public sealed class SocketTransportFactory : IConnectionListenerFactory, IConnectionListenerFactorySelector
{
    private readonly SocketTransportOptions _options;
    private readonly ILoggerFactory _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SocketTransportFactory"/> class.
    /// </summary>
    /// <param name="options">The transport options.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    public SocketTransportFactory(
        IOptions<SocketTransportOptions> options,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _options = options.Value;
        _logger = loggerFactory;
    }

    /// <inheritdoc />
    public ValueTask<IConnectionListener> BindAsync(EndPoint endpoint, CancellationToken cancellationToken = default)
    {
        var transport = new SocketConnectionListener(endpoint, _options, _logger);
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
}
