// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.Cookies;

/// <summary>
/// Context object passed to the <see cref="CookieAuthenticationEvents.SigningOut(CookieSigningOutContext)"/>
/// </summary>
public class CookieSigningOutContext : PropertiesContext<CookieAuthenticationOptions>
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="context"></param>
    /// <param name="scheme"></param>
    /// <param name="options"></param>
    /// <param name="properties"></param>
    /// <param name="cookieOptions"></param>
    public CookieSigningOutContext(
        HttpContext context,
        AuthenticationScheme scheme,
        CookieAuthenticationOptions options,
        AuthenticationProperties? properties,
        CookieOptions cookieOptions)
        : base(context, scheme, options, properties)
        => CookieOptions = cookieOptions;

    /// <summary>
    /// The options for creating the outgoing cookie.
    /// May be replace or altered during the SigningOut call.
    /// </summary>
    public CookieOptions CookieOptions { get; set; }
}
