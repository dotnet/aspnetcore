// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Security;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Features;

/// <summary>
/// Feature to get access to the connection's <see cref="SslStream" />.
/// This feature will not be available for non-TLS connections, for HTTP/3, or for connections
/// whose TLS is handled without an <see cref="SslStream"/>.
/// </summary>
public interface ISslStreamFeature
{
    /// <summary>
    /// Gets the <see cref="SslStream"/>.
    /// Note that <see cref="ISslStreamFeature"/> will not be available for non-TLS connections,
    /// for HTTP/3, or when the connection's TLS is not backed by an <see cref="SslStream"/> -
    /// consult <c>ITlsHandshakeFeature</c> and <c>ITlsConnectionFeature</c> for the negotiated
    /// TLS values instead, as those are available on every TLS connection.
    /// </summary>
    SslStream SslStream { get; }
}
