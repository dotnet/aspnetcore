// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Connections;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls;

/// <summary>
/// A callback invoked with the raw ClientHello record bytes as soon as they are parsed, before the
/// handshake completes. Intended for observation only (for example, TLS fingerprinting); it cannot alter
/// or reject the handshake.
/// </summary>
/// <param name="connection">The connection whose ClientHello was received.</param>
/// <param name="clientHelloBytes">
/// The raw ClientHello record bytes. The span is only valid for the duration of the callback; copy it if
/// the data must outlive the call.
/// </param>
[Experimental("ASPNETCORE_DIRECTTLS_001", UrlFormat = "https://aka.ms/aspnetcore/directtls")]
public delegate void DirectTlsClientHelloBytesCallback(ConnectionContext connection, ReadOnlySpan<byte> clientHelloBytes);
