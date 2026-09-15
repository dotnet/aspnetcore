// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls;

/// <summary>
/// An <see cref="IPEndPoint"/> that opts an endpoint into the DirectTls transport. Bind it with
/// <c>KestrelServerOptions.Listen(...)</c> (or <c>ListenAnyIP</c>/<c>ListenLocalhost</c>) to serve that
/// endpoint over the native, file-descriptor-bound OpenSSL TLS session instead of <c>SslStream</c>.
/// </summary>
/// <remarks>
/// <para>
/// DirectTls is TLS-only. The endpoint's <see cref="Options"/> must supply a server certificate (via
/// <see cref="DirectTlsEndpointOptions.ServerCertificate"/> or
/// <see cref="DirectTlsEndpointOptions.ServerCertificateSelector"/>).
/// </para>
/// </remarks>
[Experimental("ASPNETCORE_DIRECTTLS_001", UrlFormat = "https://aka.ms/aspnetcore/directtls")]
public sealed class DirectTlsEndpoint : IPEndPoint
{
    /// <summary>
    /// Initializes a new <see cref="DirectTlsEndpoint"/> with a default <see cref="DirectTlsEndpointOptions"/>.
    /// </summary>
    /// <param name="address">The IP address to listen on.</param>
    /// <param name="port">The port to listen on.</param>
    public DirectTlsEndpoint(IPAddress address, int port)
        : this(address, port, new DirectTlsEndpointOptions())
    {
    }

    /// <summary>
    /// Initializes a new <see cref="DirectTlsEndpoint"/> with the specified TLS options.
    /// </summary>
    /// <param name="address">The IP address to listen on.</param>
    /// <param name="port">The port to listen on.</param>
    /// <param name="options">The per-endpoint TLS configuration.</param>
    public DirectTlsEndpoint(IPAddress address, int port, DirectTlsEndpointOptions options)
        : base(address, port)
    {
        ArgumentNullException.ThrowIfNull(options);
        Options = options;
    }

    /// <summary>
    /// Initializes a new <see cref="DirectTlsEndpoint"/> from an existing <see cref="IPEndPoint"/> with the specified TLS options.
    /// </summary>
    /// <param name="endpoint">The IP endpoint (address and port) to listen on.</param>
    /// <param name="options">The per-endpoint TLS configuration.</param>
    public DirectTlsEndpoint(IPEndPoint endpoint, DirectTlsEndpointOptions options)
        : this((endpoint ?? throw new ArgumentNullException(nameof(endpoint))).Address, endpoint.Port, options)
    {
    }

    /// <summary>
    /// The per-endpoint TLS configuration for this endpoint.
    /// </summary>
    public DirectTlsEndpointOptions Options { get; }
}
