// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls;

/// <summary>
/// Per-endpoint TLS configuration for an endpoint served by the DirectTls transport. Attach an instance
/// to a <see cref="DirectTlsEndpoint"/> to tell Kestrel how to terminate TLS for that endpoint using the
/// native, file-descriptor-bound OpenSSL session (rather than <see cref="SslStream"/>).
/// </summary>
/// <remarks>
/// DirectTls is TLS-only: every endpoint served by this transport terminates TLS, so a server certificate
/// (either <see cref="ServerCertificate"/> or <see cref="ServerCertificateSelector"/>) is required.
/// </remarks>
[Experimental("ASPNETCORE_DIRECTTLS_001", UrlFormat = "https://aka.ms/aspnetcore/directtls")]
public sealed class DirectTlsEndpointOptions
{
    /// <summary>
    /// The server certificate presented during the TLS handshake. Used when
    /// <see cref="ServerCertificateSelector"/> is not set or returns <see langword="null"/>.
    /// </summary>
    public X509Certificate2? ServerCertificate { get; set; }

    /// <summary>
    /// A callback that selects the server certificate based on the connection and the SNI host name parsed
    /// from the ClientHello. When set, it is invoked once per connection before the certificate is installed;
    /// returning <see langword="null"/> falls back to <see cref="ServerCertificate"/>.
    /// </summary>
    /// <remarks>
    /// The first argument is the <see cref="ConnectionContext"/> for the connection being negotiated (already
    /// allocated so it carries the same connection id that will later serve the request); the second is the
    /// requested SNI host name, or <see langword="null"/> when the client did not send one.
    /// </remarks>
    public Func<ConnectionContext?, string?, X509Certificate2?>? ServerCertificateSelector { get; set; }

    /// <summary>
    /// The allowable TLS protocol versions. <see cref="SslProtocols.None"/> (the default) lets the operating
    /// system choose an appropriate default set.
    /// </summary>
    public SslProtocols SslProtocols { get; set; } = SslProtocols.None;

    /// <summary>
    /// Whether a client certificate is requested and/or required during the handshake (mutual TLS).
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="ClientCertificateMode.NoCertificate"/>. <see cref="ClientCertificateMode.DelayCertificate"/>
    /// is not supported by this transport.
    /// </remarks>
    public ClientCertificateMode ClientCertificateMode { get; set; } = ClientCertificateMode.NoCertificate;

    /// <summary>
    /// A callback used to validate a client certificate when <see cref="ClientCertificateMode"/> requests one.
    /// Return <see langword="true"/> to accept the certificate. When not set, a certificate is accepted only
    /// when it produced no <see cref="SslPolicyErrors"/>.
    /// </summary>
    public Func<X509Certificate2, X509Chain?, SslPolicyErrors, bool>? ClientCertificateValidation { get; set; }

    /// <summary>
    /// An optional callback invoked with the raw ClientHello record bytes as soon as they are parsed, before
    /// the handshake completes. Intended for observation only (for example, TLS fingerprinting); it cannot
    /// alter or reject the handshake.
    /// </summary>
    /// <remarks>
    /// The <see cref="ReadOnlySpan{T}"/> is valid only for the duration of the callback; copy the bytes if
    /// they must outlive the call. The second argument is the <see cref="ConnectionContext"/> for the
    /// connection being negotiated.
    /// </remarks>
    public ReadOnlySpanAction<byte, ConnectionContext>? TlsClientHelloBytesCallback { get; set; }

    /// <summary>
    /// The HTTP protocols (ALPN) advertised for this endpoint, sourced from <see cref="ListenOptions.Protocols"/>
    /// after the endpoint has been configured. Not part of the public surface: it is set by
    /// <see cref="DirectTlsEndpointProtocolsSetup"/> so the transport does not need to depend on
    /// <see cref="KestrelServerOptions"/>.
    /// </summary>
    internal HttpProtocols HttpProtocols { get; set; } = HttpProtocols.Http1AndHttp2;
}
