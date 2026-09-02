// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.AspNetCore.Http.Features;

/// <summary>
/// Provides access to TLS features associated with the current HTTP connection.
/// </summary>
public interface ITlsConnectionFeature
{
    /// <summary>
    /// Synchronously retrieves the client certificate, if any.
    /// </summary>
    X509Certificate2? ClientCertificate { get; set; }

    /// <summary>
    /// Asynchronously retrieves the client certificate, if any.
    /// </summary>
    Task<X509Certificate2?> GetClientCertificateAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Attempts to retrieve the RFC 5929 TLS channel binding token (CBT) bytes for the
    /// requested <paramref name="kind"/> from the current connection.
    /// </summary>
    /// <param name="kind">The kind of channel binding to retrieve.</param>
    /// <param name="channelBindingToken">
    /// When this method returns <see langword="true"/>, contains the channel binding token
    /// bytes; otherwise, an empty <see cref="ReadOnlyMemory{T}"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the requested channel binding is available;
    /// <see langword="false"/> otherwise (for example: the connection is not TLS,
    /// the requested <paramref name="kind"/> is not supported by the server, or
    /// channel binding is not enabled in server configuration).
    /// </returns>
    /// <remarks>
    /// <para>
    /// Channel binding tokens let in-channel authentication protocols (such as
    /// Kerberos or NTLM negotiated over HTTPS) bind themselves cryptographically
    /// to the underlying TLS channel, mitigating authentication relay attacks.
    /// See <see href="https://datatracker.ietf.org/doc/html/rfc5929"/> for the
    /// specification and Microsoft's "Extended Protection for Authentication"
    /// documentation for the Windows usage model.
    /// </para>
    /// </remarks>
    bool TryGetChannelBindingBytes(ChannelBindingKind kind, out ReadOnlyMemory<byte> channelBindingToken)
    {
        channelBindingToken = default;
        return false;
    }
}
