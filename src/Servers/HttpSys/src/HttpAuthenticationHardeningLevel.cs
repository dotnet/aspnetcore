// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.HttpSys;

/// <summary>
/// Specifies the Http.Sys authentication hardening level that controls how strictly
/// HTTP authentication (Kerberos/NTLM) is validated against the underlying TLS channel.
/// </summary>
/// <remarks>
/// <para>
/// Corresponds to the Win32
/// <see href="https://learn.microsoft.com/windows/win32/api/http/ne-http-http_authentication_hardening_levels"><c>HTTP_AUTHENTICATION_HARDENING_LEVELS</c></see>
/// enumeration applied to the URL group's <c>HttpServerChannelBindProperty</c>.
/// </para>
/// <para>
/// When set to <see cref="Medium"/> or <see cref="Strict"/>, Http.Sys is also
/// instructed to attach the per-request <c>HTTP_REQUEST_CHANNEL_BIND_STATUS</c>
/// so the application can retrieve the RFC 5929 TLS channel binding token via
/// <see cref="Microsoft.AspNetCore.Http.Features.ITlsConnectionFeature.TryGetChannelBindingBytes"/>.
/// </para>
/// </remarks>
public enum HttpAuthenticationHardeningLevel
{
    /// <summary>
    /// Http.Sys does not enforce channel binding validation and does not expose the RFC 5929 TLS channel binding token to the application.
    /// This matches the pre-hardening default behavior.
    /// </summary>
    Legacy = 0,

    /// <summary>
    /// Http.Sys validates channel binding tokens when clients supply them but tolerates their absence.
    /// The per-request TLS channel binding token is exposed to the application.
    /// </summary>
    Medium = 1,

    /// <summary>
    /// Http.Sys requires channel binding tokens on authenticated requests and rejects those without one.
    /// The per-request TLS channel binding token is exposed to the application.
    /// </summary>
    Strict = 2,
}
