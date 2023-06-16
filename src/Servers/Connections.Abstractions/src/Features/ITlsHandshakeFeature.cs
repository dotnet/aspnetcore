// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Authentication;

#if NETCOREAPP
using System.Net.Security;
#endif

namespace Microsoft.AspNetCore.Connections.Features;

/// <summary>
/// Represents the details about the TLS handshake.
/// </summary>
public interface ITlsHandshakeFeature
{
    /// <summary>
    /// Gets the <see cref="SslProtocols"/>.
    /// </summary>
    SslProtocols Protocol { get; }

#if NETCOREAPP
    /// <summary>
    /// Gets the <see cref="TlsCipherSuite"/>.
    /// </summary>
    TlsCipherSuite? NegotiatedCipherSuite => null;

    /// <summary>
    /// Gets the host name from the "server_name" (SNI) extension of the client hello if present.
    /// See <see href="https://www.rfc-editor.org/rfc/rfc6066#section-3">RFC 6066</see>.
    /// </summary>
    string HostName => string.Empty;
#endif

    /// <summary>
    /// Gets the <see cref="CipherAlgorithmType"/>.
    /// </summary>
    CipherAlgorithmType CipherAlgorithm { get; }

    /// <summary>
    /// Gets the cipher strength.
    /// </summary>
    int CipherStrength { get; }

    /// <summary>
    /// Gets the <see cref="HashAlgorithmType"/>.
    /// </summary>
    HashAlgorithmType HashAlgorithm { get; }

    /// <summary>
    /// Gets the hash strength.
    /// </summary>
    int HashStrength { get; }

    /// <summary>
    /// Gets the <see cref="KeyExchangeAlgorithm"/>.
    /// </summary>
    ExchangeAlgorithmType KeyExchangeAlgorithm { get; }

    /// <summary>
    /// Gets the key exchange algorithm strength.
    /// </summary>
    int KeyExchangeStrength { get; }
}
