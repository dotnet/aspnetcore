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
/// Per-endpoint TLS configuration for an endpoint served by the DirectTls transport.
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
    /// <para>
    /// The callback is invoked on the <see cref="ThreadPool"/>, so a slow one does not hold up the handshakes or
    /// I/O of other connections. It does still delay this connection, and the time spent in it counts against
    /// <see cref="HandshakeTimeout"/> - a callback that overruns that budget costs this connection its handshake.
    /// Prefer fast, non-blocking work regardless: each concurrent invocation occupies a thread-pool thread.
    /// </para>
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
    /// Defaults to <see cref="ClientCertificateMode.NoCertificate"/>.
    /// <see cref="ClientCertificateMode.DelayCertificate"/> is not supported (see https://github.com/dotnet/aspnetcore/issues/67915)
    /// </remarks>
    public ClientCertificateMode ClientCertificateMode { get; set; } = ClientCertificateMode.NoCertificate;

    /// <summary>
    /// A callback used to validate a client certificate when <see cref="ClientCertificateMode"/> requests one.
    /// Return <see langword="true"/> to accept the certificate. When not set, a certificate is accepted only
    /// when it produced no <see cref="SslPolicyErrors"/>.
    /// </summary>
    /// <remarks>
    /// The callback is invoked on the <see cref="ThreadPool"/>, so a slow one does not hold up the handshakes or
    /// I/O of other connections. It does still delay this connection, and the time spent in it counts against
    /// <see cref="HandshakeTimeout"/> - a callback that overruns that budget costs this connection its handshake.
    /// Prefer fast, non-blocking work regardless: each concurrent invocation occupies a thread-pool thread.
    /// </remarks>
    public Func<X509Certificate2, X509Chain?, SslPolicyErrors, bool>? ClientCertificateValidation { get; set; }

    /// <summary>
    /// An optional callback invoked with the raw ClientHello record bytes as soon as they are parsed, before
    /// the handshake completes.
    /// </summary>
    /// <remarks>
    /// The <see cref="ReadOnlySequence{T}"/> is only valid for the duration of the callback; copy the bytes
    /// (for example with <c>ToArray()</c>) if they must outlive the call. The first argument is the
    /// <see cref="ConnectionContext"/> for the connection being negotiated.
    /// <para>
    /// The callback is invoked on the <see cref="ThreadPool"/>, so a slow one does not hold up the handshakes or
    /// I/O of other connections. It does still delay this connection, and the time spent in it counts against
    /// <see cref="HandshakeTimeout"/> - a callback that overruns that budget costs this connection its handshake.
    /// Prefer fast, non-blocking work regardless: each concurrent invocation occupies a thread-pool thread.
    /// </para>
    /// </remarks>
    public Action<ConnectionContext, ReadOnlySequence<byte>>? TlsClientHelloBytesCallback { get; set; }

    /// <summary>
    /// Overrides the transport-wide worker count (<c>DirectTlsTransportOptions.WorkerCount</c>) for this
    /// endpoint. When <see langword="null"/> (the default), the transport-wide worker count is used.
    /// </summary>
    /// <remarks>
    /// Each DirectTls endpoint runs its own pool of worker threads, so a server's total thread count is the sum
    /// of every bound endpoint's worker count. Set this on individual endpoints to bound threads when hosting
    /// several DirectTls endpoints — for example, a low-traffic management port can use far fewer workers than a
    /// public HTTPS port. Must be greater than zero when set.
    /// </remarks>
    public int? WorkerCount
    {
        get;
        set
        {
            if (value is <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, $"{nameof(WorkerCount)} must be greater than zero when set.");
            }

            field = value;
        }
    }

    /// <summary>
    /// The maximum amount of time allowed for the TLS handshake to complete on a connection to this endpoint.
    /// A connection whose handshake does not finish within this window is dropped. Defaults to 10 seconds.
    /// Set to <see cref="Timeout.InfiniteTimeSpan"/> to disable the timeout; any other non-positive value is rejected.
    /// </summary>
    /// <remarks>
    /// This bounds slow or stalled handshakes — for example a client that opens a connection and then dribbles
    /// the ClientHello one byte at a time — which would otherwise keep a file descriptor and its native TLS
    /// session pinned to a worker indefinitely. It mirrors <see cref="HttpsConnectionAdapterOptions.HandshakeTimeout"/>,
    /// which provides the same protection for the <see cref="SslStream"/>-based HTTPS middleware, and shares its
    /// 10-second default.
    /// </remarks>
    public TimeSpan HandshakeTimeout
    {
        get;
        set
        {
            if (value <= TimeSpan.Zero && value != Timeout.InfiniteTimeSpan)
            {
                throw new ArgumentOutOfRangeException(nameof(value), CoreStrings.PositiveTimeSpanRequired);
            }

            field = value != Timeout.InfiniteTimeSpan ? value : TimeSpan.MaxValue;
        }
    } = HttpsConnectionAdapterOptions.DefaultHandshakeTimeout;

    /// <summary>
    /// The HTTP protocols (ALPN) advertised for this endpoint,
    /// sourced from <see cref="ListenOptions.Protocols"/> after the endpoint has been configured. 
    /// </summary>
    internal HttpProtocols HttpProtocols { get; set; } = HttpProtocols.Http1AndHttp2;
}
