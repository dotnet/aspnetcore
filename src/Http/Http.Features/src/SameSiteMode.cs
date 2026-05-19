// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Used to set the SameSite field on response cookies to indicate if those cookies should be included by the client on future "same-site" or "cross-site" requests.
/// RFC Draft: <see href="https://tools.ietf.org/html/draft-ietf-httpbis-rfc6265bis-03#section-4.1.1"/>
/// </summary>
// This mirrors Microsoft.Net.Http.Headers.SameSiteMode
public enum SameSiteMode
{
    /// <summary>No SameSite field will be set, the client should follow its default cookie policy.</summary>
    Unspecified = -1,
    /// <summary>Indicates the client should disable same-site restrictions.</summary>
    None = 0,
    /// <summary>Indicates the client should send the cookie with "same-site" requests, and with "cross-site" top-level navigations.</summary>
    Lax,
    /// <summary>Indicates the client should only send the cookie with "same-site" requests.</summary>
    Strict
}
