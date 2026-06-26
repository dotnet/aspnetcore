// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Authentication.ExtendedProtection;

namespace Microsoft.AspNetCore.Http.Features;

/// <summary>
/// Provides access to RFC 5929 TLS channel binding tokens (CBT) associated
/// with the current connection.
/// </summary>
/// <remarks>
/// <para>
/// Channel binding tokens let in-channel authentication protocols (such as
/// Kerberos or NTLM negotiated over HTTPS) bind themselves cryptographically
/// to the underlying TLS channel, mitigating authentication relay attacks.
/// See <see href="https://datatracker.ietf.org/doc/html/rfc5929"/> for the
/// specification and Microsoft's "Extended Protection for Authentication"
/// documentation for the Windows usage model.
/// </para>
/// <para>
/// The returned bytes form the same SSPI/GSS-API compatible
/// <c>SEC_CHANNEL_BINDINGS</c> blob produced by
/// <see cref="System.Net.TransportContext.GetChannelBinding(ChannelBindingKind)"/>
/// and can be wrapped in a
/// <see cref="System.Security.Authentication.ExtendedProtection.ChannelBinding"/>
/// to be passed to <c>AcceptSecurityContext</c>, or forwarded to a backend
/// server (for example by a TLS-terminating reverse proxy) so the backend
/// can validate authentication relayed over the front-end TLS channel.
/// </para>
/// </remarks>
public interface ITlsChannelBindingFeature
{
    /// <summary>
    /// Retrieves the channel binding token bytes for the requested
    /// <paramref name="kind"/>, or <see langword="null"/> if the server
    /// cannot provide one for the current connection.
    /// </summary>
    /// <param name="kind">The kind of channel binding to retrieve.</param>
    /// <returns>
    /// The channel binding token bytes, or <see langword="null"/> if
    /// unavailable (for example: the connection is not TLS, the requested
    /// kind is not supported by the server, or channel binding has not been
    /// enabled in server configuration).
    /// </returns>
    ReadOnlyMemory<byte>? GetChannelBindingBytes(ChannelBindingKind kind);
}
