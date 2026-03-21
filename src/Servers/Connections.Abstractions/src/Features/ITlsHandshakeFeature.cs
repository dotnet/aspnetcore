// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Authentication;
using System.Security.Cryptography;
using System;
using Microsoft.AspNetCore.Shared;

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
    [Obsolete(Obsoletions.RuntimeTlsCipherAlgorithmEnumsMessage, DiagnosticId = Obsoletions.RuntimeTlsCipherAlgorithmEnumsDiagId, UrlFormat = Obsoletions.RuntimeSharedUrlFormat)]
#endif
    CipherAlgorithmType CipherAlgorithm { get; }

    /// <summary>
    /// Gets the cipher strength.
    /// </summary>
#if NETCOREAPP
    [Obsolete(Obsoletions.RuntimeTlsCipherAlgorithmEnumsMessage, DiagnosticId = Obsoletions.RuntimeTlsCipherAlgorithmEnumsDiagId, UrlFormat = Obsoletions.RuntimeSharedUrlFormat)]
#endif
    int CipherStrength { get; }

    /// <summary>
    /// Gets the <see cref="HashAlgorithmType"/>.
    /// </summary>
#if NETCOREAPP
    [Obsolete(Obsoletions.RuntimeTlsCipherAlgorithmEnumsMessage, DiagnosticId = Obsoletions.RuntimeTlsCipherAlgorithmEnumsDiagId, UrlFormat = Obsoletions.RuntimeSharedUrlFormat)]
#endif
    HashAlgorithmType HashAlgorithm { get; }

    /// <summary>
    /// Gets the hash strength.
    /// </summary>
#if NETCOREAPP
    [Obsolete(Obsoletions.RuntimeTlsCipherAlgorithmEnumsMessage, DiagnosticId = Obsoletions.RuntimeTlsCipherAlgorithmEnumsDiagId, UrlFormat = Obsoletions.RuntimeSharedUrlFormat)]
#endif
    int HashStrength { get; }

    /// <summary>
    /// Gets the <see cref="ExchangeAlgorithmType"/>.
    /// </summary>
#if NETCOREAPP
    [Obsolete(Obsoletions.RuntimeTlsCipherAlgorithmEnumsMessage, DiagnosticId = Obsoletions.RuntimeTlsCipherAlgorithmEnumsDiagId, UrlFormat = Obsoletions.RuntimeSharedUrlFormat)]
#endif
    ExchangeAlgorithmType KeyExchangeAlgorithm { get; }

    /// <summary>
    /// Gets the key exchange algorithm strength.
    /// </summary>
#if NETCOREAPP
    [Obsolete(Obsoletions.RuntimeTlsCipherAlgorithmEnumsMessage, DiagnosticId = Obsoletions.RuntimeTlsCipherAlgorithmEnumsDiagId, UrlFormat = Obsoletions.RuntimeSharedUrlFormat)]
#endif
    int KeyExchangeStrength { get; }
}
