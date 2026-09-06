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
/// </summary>
internal sealed partial class DirectTlsConnection : ITlsConnectionFeature, ITlsHandshakeFeature, ITlsApplicationProtocolFeature, IConnectionSocketFeature
{
    private Socket? _socket;
    private readonly object _socketLock = new();

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
    /// NOT IDEAL - this is a sharp edge (see the sample app README). DirectTls does not retain the managed
    /// <see cref="Socket"/> returned by accept: ownership of its fd is transferred to the
    /// <see cref="TlsSocketSession"/>'s <see cref="SafeSocketHandle"/> and driven by the TLS event pump. We
    /// reconstruct a <see cref="Socket"/> from the raw file descriptor on demand, wrapping it in a
    /// <b>non-owning</b> <see cref="SafeSocketHandle"/> so that disposing the returned socket does not close the
    /// descriptor the TLS session still uses. The returned socket is only safe for reading metadata (endpoints,
    /// socket options); performing raw send/receive on it would corrupt the TLS record stream. It exists purely
    /// for feature parity with the standard sockets transport, which surfaces the real accepted socket here.
    /// </summary>
    public Socket Socket
    {
        get
        {
            lock (_socketLock)
            {
                if (_socket is { } existing)
                {
                    return existing;
                }

                ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);

                _socket = new Socket(new SafeSocketHandle((IntPtr)_connectionState.Fd, ownsHandle: false));
                return _socket;
            }
        }
    }
}
