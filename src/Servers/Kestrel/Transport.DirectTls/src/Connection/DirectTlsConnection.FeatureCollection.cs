// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls.Connection;

/// <summary>
/// Feature-collection surface for a DirectTls connection, implemented directly on the connection object.
///
/// This mirrors two existing patterns in the codebase:
/// <list type="bullet">
///   <item><c>SocketConnection.FeatureCollection.cs</c>, which implements <see cref="IConnectionSocketFeature"/>
///   on the connection itself and reads from its private <c>Socket</c>.</item>
///   <item>The SslStream path's <c>TlsConnectionFeature</c>, a single object that backs every TLS feature by
///   reading live from one underlying <see cref="System.Net.Security.SslStream"/>.</item>
/// </list>
///
/// Here the single source of truth is the <see cref="System.Net.Security.TlsSocketSession"/> driving the
/// connection's <see cref="ConnectionIoState"/>. The negotiated
/// TLS values are immutable once the handshake completes (which is always the case by the time these features
/// are read on the request path), so they are read live rather than snapshotted. The one exception is the
/// client certificate, which is captured and validated on the pump thread at handshake completion and stored
/// in <see cref="ClientCertificate"/>. DirectTls terminates TLS for every connection, so the
/// <see cref="TlsSession"/> accessor is non-null whenever these features are read.
/// </summary>
internal sealed partial class DirectTlsConnection : ITlsConnectionFeature, ITlsHandshakeFeature, ITlsApplicationProtocolFeature, IConnectionSocketFeature
{
    private Socket? _socket;

    // The ALPN protocol negotiated during the handshake (empty when none was negotiated). Set in the
    // constructor and again at CompleteHandshake once the handshake has actually completed.
    private SslApplicationProtocol _negotiatedApplicationProtocol;

    // The TLS session backing this connection. DirectTls terminates TLS for every connection.
    private TlsSocketSession TlsSession => _connectionState.Session;

    // ── ITlsConnectionFeature ────────────────────────────────────────────────
    // Present on the connection so the UseHttps middleware no-ops (it does not wrap the already-encrypted
    // transport in a second SslStream) and Kestrel resolves the request scheme as https.

    /// <summary>
    /// The client certificate presented during the (mutual-TLS) handshake, or <see langword="null"/> when the
    /// endpoint did not request one or the peer did not present one. It is captured and validated at handshake
    /// completion by the TLS event pump; it is not re-queried from the native session on read.
    /// </summary>
    public X509Certificate2? ClientCertificate { get; set; }

    /// <inheritdoc />
    public Task<X509Certificate2?> GetClientCertificateAsync(CancellationToken cancellationToken)
        => Task.FromResult(ClientCertificate);

    // ── ITlsHandshakeFeature ─────────────────────────────────────────────────

    /// <inheritdoc />
    public SslProtocols Protocol => TlsSession.NegotiatedProtocol;

    /// <inheritdoc />
    public TlsCipherSuite? NegotiatedCipherSuite => TlsSession.NegotiatedCipherSuite;

    /// <inheritdoc />
    public string HostName => TlsSession.TargetHostName ?? string.Empty;

    // The legacy Cipher/Hash/KeyExchange algorithm triples are obsolete and report their neutral values on
    // modern TLS (1.2/1.3) - exactly as SslStream does. Operators should read NegotiatedCipherSuite instead.
#pragma warning disable SYSLIB0058 // Obsolete TLS cipher algorithm enums
    /// <inheritdoc />
    public CipherAlgorithmType CipherAlgorithm => CipherAlgorithmType.None;

    /// <inheritdoc />
    public int CipherStrength => 0;

    /// <inheritdoc />
    public HashAlgorithmType HashAlgorithm => HashAlgorithmType.None;

    /// <inheritdoc />
    public int HashStrength => 0;

    /// <inheritdoc />
    public ExchangeAlgorithmType KeyExchangeAlgorithm => ExchangeAlgorithmType.None;

    /// <inheritdoc />
    public int KeyExchangeStrength => 0;
#pragma warning restore SYSLIB0058

    // ── ITlsApplicationProtocolFeature ───────────────────────────────────────
    // Published directly (this transport references Kestrel.Core), so HttpConnection.SelectProtocol can
    // negotiate HTTP/2 without the UseHttps middleware bridging a raw SslApplicationProtocol feature.

    /// <inheritdoc />
    public ReadOnlyMemory<byte> ApplicationProtocol => _negotiatedApplicationProtocol.Protocol;

    // ── IConnectionSocketFeature ─────────────────────────────────────────────

    /// <summary>
    /// Exposes a managed <see cref="Socket"/> for the connection.
    ///
    /// NOT IDEAL - this is a sharp edge (see the sample app README). DirectTls never owns a managed
    /// <see cref="Socket"/>: the accepted socket's <see cref="SafeSocketHandle"/> is handed straight to OpenSSL
    /// (via <c>SSL_set_fd</c>) and driven by the TLS event pump. We reconstruct a <see cref="Socket"/> from the
    /// raw file descriptor on demand, wrapping it in a <b>non-owning</b> <see cref="SafeSocketHandle"/> so that
    /// disposing the returned socket does not close the descriptor the TLS session still uses. The returned
    /// socket is only safe for reading metadata (endpoints, socket options); performing raw send/receive on it
    /// would corrupt the TLS record stream. It exists purely for feature parity with the standard sockets
    /// transport, which surfaces the real accepted socket here.
    /// </summary>
    public Socket Socket
        => _socket ??= new Socket(new SafeSocketHandle((IntPtr)_connectionState.Fd, ownsHandle: false));
}
