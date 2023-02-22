// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Authentication;

#if NET7_0_OR_GREATER
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

#if NET7_0_OR_GREATER
    /// <summary>
    /// Gets the <see cref="TlsCipherSuite"/>.
    /// </summary>
    TlsCipherSuite? NegotiatedCipherSuite { get => default; }

    /// <summary>
    /// Gets the <see cref="SslStream"/>.
    /// </summary>
    SslStream? SslStream { get => default; }
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
