// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Obsoletions = Microsoft.AspNetCore.Shared.Obsoletions;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal;

internal sealed class TlsConnectionFeature : ITlsConnectionFeature, ITlsApplicationProtocolFeature, ITlsHandshakeFeature, ISslStreamFeature
{
    private readonly SslStream _sslStream;
    private readonly ConnectionContext _context;
    private bool _snapshotted;

    private X509Certificate2? _clientCert;
    private Task<X509Certificate2?>? _clientCertTask;

    private SslProtocols _protocol;
    private TlsCipherSuite? _negotiatedCipherSuite;
    private ReadOnlyMemory<byte> _applicationProtocol;
#pragma warning disable SYSLIB0058 // Obsolete TLS cipher algorithm enums
    private CipherAlgorithmType _cipherAlgorithm;
    private int _cipherStrength;
    private HashAlgorithmType _hashAlgorithm;
    private int _hashStrength;
    private ExchangeAlgorithmType _keyExchangeAlgorithm;
    private int _keyExchangeStrength;
#pragma warning restore SYSLIB0058

    public TlsConnectionFeature(SslStream sslStream, ConnectionContext context)
    {
        ArgumentNullException.ThrowIfNull(sslStream);
        ArgumentNullException.ThrowIfNull(context);

        _sslStream = sslStream;
        _context = context;
    }

    /// <summary>
    /// Captures all SslStream-backed property values so they remain accessible after the SslStream is disposed.
    /// Must be called before disposing the SslStream.
    /// </summary>
    internal void Snapshot()
    {
        if (_snapshotted)
        {
            return;
        }
        _snapshotted = true;

        if (_sslStream is null)
        {
            return;
        }

        try
        {
            _protocol = _sslStream.SslProtocol;
            _negotiatedCipherSuite = _sslStream.NegotiatedCipherSuite;
            _applicationProtocol = _sslStream.NegotiatedApplicationProtocol.Protocol.ToArray();

#pragma warning disable SYSLIB0058 // Obsolete TLS cipher algorithm enums
            _cipherAlgorithm = _sslStream.CipherAlgorithm;
            _cipherStrength = _sslStream.CipherStrength;
            _hashAlgorithm = _sslStream.HashAlgorithm;
            _hashStrength = _sslStream.HashStrength;
            _keyExchangeAlgorithm = _sslStream.KeyExchangeAlgorithm;
            _keyExchangeStrength = _sslStream.KeyExchangeStrength;
#pragma warning restore SYSLIB0058

            _clientCert ??= ConvertToX509Certificate2(_sslStream.RemoteCertificate);
        }
        catch
        {
            // If the handshake never completed, SslStream properties may throw.
            // The snapshotted fields will retain their default values.
        }
    }

    internal bool AllowDelayedClientCertificateNegotation { get; set; }

    public X509Certificate2? ClientCertificate
    {
        get
        {
            return _clientCert ??= ConvertToX509Certificate2(_sslStream.RemoteCertificate);
        }
        set
        {
            _clientCert = value;
            _clientCertTask = Task.FromResult(value);
        }
    }

    public string HostName { get; set; } = string.Empty;

    public ReadOnlyMemory<byte> ApplicationProtocol => _snapshotted ? _applicationProtocol : _sslStream.NegotiatedApplicationProtocol.Protocol;

    public SslProtocols Protocol => _snapshotted ? _protocol : _sslStream.SslProtocol;

    public SslStream SslStream => _sslStream;

    public Exception? Exception { get; set; }

    // After Snapshot() is called, all values are served from cached fields instead of the SslStream.

    public TlsCipherSuite? NegotiatedCipherSuite => _snapshotted ? _negotiatedCipherSuite : _sslStream.NegotiatedCipherSuite;

    [Obsolete(Obsoletions.RuntimeTlsCipherAlgorithmEnumsMessage, DiagnosticId = Obsoletions.RuntimeTlsCipherAlgorithmEnumsDiagId, UrlFormat = Obsoletions.RuntimeSharedUrlFormat)]
    public CipherAlgorithmType CipherAlgorithm => _snapshotted ? _cipherAlgorithm : _sslStream.CipherAlgorithm;

    [Obsolete(Obsoletions.RuntimeTlsCipherAlgorithmEnumsMessage, DiagnosticId = Obsoletions.RuntimeTlsCipherAlgorithmEnumsDiagId, UrlFormat = Obsoletions.RuntimeSharedUrlFormat)]
    public int CipherStrength => _snapshotted ? _cipherStrength : _sslStream.CipherStrength;

    [Obsolete(Obsoletions.RuntimeTlsCipherAlgorithmEnumsMessage, DiagnosticId = Obsoletions.RuntimeTlsCipherAlgorithmEnumsDiagId, UrlFormat = Obsoletions.RuntimeSharedUrlFormat)]
    public HashAlgorithmType HashAlgorithm => _snapshotted ? _hashAlgorithm : _sslStream.HashAlgorithm;

    [Obsolete(Obsoletions.RuntimeTlsCipherAlgorithmEnumsMessage, DiagnosticId = Obsoletions.RuntimeTlsCipherAlgorithmEnumsDiagId, UrlFormat = Obsoletions.RuntimeSharedUrlFormat)]
    public int HashStrength => _snapshotted ? _hashStrength : _sslStream.HashStrength;

    [Obsolete(Obsoletions.RuntimeTlsCipherAlgorithmEnumsMessage, DiagnosticId = Obsoletions.RuntimeTlsCipherAlgorithmEnumsDiagId, UrlFormat = Obsoletions.RuntimeSharedUrlFormat)]
    public ExchangeAlgorithmType KeyExchangeAlgorithm => _snapshotted ? _keyExchangeAlgorithm : _sslStream.KeyExchangeAlgorithm;

    [Obsolete(Obsoletions.RuntimeTlsCipherAlgorithmEnumsMessage, DiagnosticId = Obsoletions.RuntimeTlsCipherAlgorithmEnumsDiagId, UrlFormat = Obsoletions.RuntimeSharedUrlFormat)]
    public int KeyExchangeStrength => _snapshotted ? _keyExchangeStrength : _sslStream.KeyExchangeStrength;

    public Task<X509Certificate2?> GetClientCertificateAsync(CancellationToken cancellationToken)
    {
        // Only try once per connection
        if (_clientCertTask != null)
        {
            return _clientCertTask;
        }

        if (ClientCertificate != null
            || !AllowDelayedClientCertificateNegotation
            // Delayed client cert negotiation is not allowed on HTTP/2 (or HTTP/3, but that's implemented elsewhere).
            || _sslStream.NegotiatedApplicationProtocol == SslApplicationProtocol.Http2)
        {
            return _clientCertTask = Task.FromResult(ClientCertificate);
        }

        return _clientCertTask = GetClientCertificateAsyncCore(cancellationToken);
    }

    private async Task<X509Certificate2?> GetClientCertificateAsyncCore(CancellationToken cancellationToken)
    {
        try
        {
#pragma warning disable CA1416 // Validate platform compatibility
            await _sslStream.NegotiateClientCertificateAsync(cancellationToken);
#pragma warning restore CA1416 // Validate platform compatibility
        }
        catch (PlatformNotSupportedException)
        {
            // NegotiateClientCertificateAsync might not be supported on all platforms.
            // Don't attempt to recover by creating a new connection. Instead, just throw error directly to the app.
            throw;
        }
        catch
        {
            // We can't tell which exceptions are fatal or recoverable. Consider them all recoverable only given a new connection
            // and close the connection gracefully to avoid over-caching and affecting future requests on this connection.
            // This allows recovery by starting a new connection. The close is graceful to allow the server to
            // send an error response like 401. https://github.com/dotnet/aspnetcore/issues/41369
            _context.Features.Get<IConnectionLifetimeNotificationFeature>()?.RequestClose();
            throw;
        }

        return ClientCertificate;
    }

    private static X509Certificate2? ConvertToX509Certificate2(X509Certificate? certificate)
    {
        return certificate switch
        {
            null => null,
            X509Certificate2 cert2 => cert2,
            _ => new X509Certificate2(certificate),
        };
    }
}
