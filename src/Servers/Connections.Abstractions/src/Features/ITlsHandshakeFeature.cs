// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Authentication;
using System.Security.Cryptography;
using System;
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
#if NETCOREAPP
    [Obsolete("KeyExchangeAlgorithm, KeyExchangeStrength, CipherAlgorithm, CipherStrength, HashAlgorithm and HashStrength properties of ITlsHandshakeFeature are obsolete. Use NegotiatedCipherSuite instead.")]
#endif
    CipherAlgorithmType CipherAlgorithm { get; }

    /// <summary>
    /// Gets the cipher strength.
    /// </summary>
#if NETCOREAPP
    [Obsolete("KeyExchangeAlgorithm, KeyExchangeStrength, CipherAlgorithm, CipherStrength, HashAlgorithm and HashStrength properties of ITlsHandshakeFeature are obsolete. Use NegotiatedCipherSuite instead.")]
#endif
    int CipherStrength { get; }

    /// <summary>
    /// Gets the <see cref="HashAlgorithmType"/>.
    /// </summary>
#if NETCOREAPP
    [Obsolete("KeyExchangeAlgorithm, KeyExchangeStrength, CipherAlgorithm, CipherStrength, HashAlgorithm and HashStrength properties of ITlsHandshakeFeature are obsolete. Use NegotiatedCipherSuite instead.")]
#endif
    HashAlgorithmType HashAlgorithm { get; }

    /// <summary>
    /// Gets the hash strength.
    /// </summary>
#if NETCOREAPP
    [Obsolete("KeyExchangeAlgorithm, KeyExchangeStrength, CipherAlgorithm, CipherStrength, HashAlgorithm and HashStrength properties of ITlsHandshakeFeature are obsolete. Use NegotiatedCipherSuite instead.")]
#endif
    int HashStrength { get; }

    /// <summary>
    /// Gets the <see cref="ExchangeAlgorithmType"/>.
    /// </summary>
#if NETCOREAPP
    [Obsolete("KeyExchangeAlgorithm, KeyExchangeStrength, CipherAlgorithm, CipherStrength, HashAlgorithm and HashStrength properties of ITlsHandshakeFeature are obsolete. Use NegotiatedCipherSuite instead.")]
#endif
    ExchangeAlgorithmType KeyExchangeAlgorithm { get; }

    /// <summary>
    /// Gets the key exchange algorithm strength.
    /// </summary>
#if NETCOREAPP
    [Obsolete("KeyExchangeAlgorithm, KeyExchangeStrength, CipherAlgorithm, CipherAlgorithmStrength, HashAlgorithm and HashStrength properties of ITlsHandshakeFeature are obsolete. Use NegotiatedCipherSuite instead.")]
#endif
    int KeyExchangeStrength { get; }
}
