// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Determines how cookie security properties are set.
/// </summary>
public enum CookieSecurePolicy
{
    /// <summary>
    /// If the URI that provides the cookie is HTTPS, then the cookie will only be returned to the server on
    /// subsequent HTTPS requests. Otherwise if the URI that provides the cookie is HTTP, then the cookie will
    /// be returned to the server on all HTTP and HTTPS requests. This value ensures
    /// HTTPS for all authenticated requests on deployed servers, and also supports HTTP for localhost development
    /// and for servers that do not have HTTPS support.
    /// </summary>
    SameAsRequest,

    /// <summary>
    /// Secure is always marked true. Use this value when your login page and all subsequent pages
    /// requiring the authenticated identity are HTTPS. Local development will also need to be done with HTTPS urls.
    /// </summary>
    Always,

    /// <summary>
    /// Secure is not marked true. Use this value when your login page is HTTPS, but other pages
    /// on the site which are HTTP also require authentication information. This setting is not recommended because
    /// the authentication information provided with an HTTP request may be observed and used by other computers
    /// on your local network or wireless connection.
    /// </summary>
    None,
}
