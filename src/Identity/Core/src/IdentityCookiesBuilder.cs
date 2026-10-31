// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Used to configure identity cookie options.
/// </summary>
public class IdentityCookiesBuilder
{
    /// <summary>
    /// Used to configure the application cookie.
    /// </summary>
    public OptionsBuilder<CookieAuthenticationOptions>? ApplicationCookie { get; set; }

    /// <summary>
    /// Used to configure the external cookie.
    /// </summary>
    public OptionsBuilder<CookieAuthenticationOptions>? ExternalCookie { get; set; }

    /// <summary>
    /// Used to configure the two factor remember me cookie.
    /// </summary>
    public OptionsBuilder<CookieAuthenticationOptions>? TwoFactorRememberMeCookie { get; set; }

    /// <summary>
    /// Used to configure the two factor user id cookie.
    /// </summary>
    public OptionsBuilder<CookieAuthenticationOptions>? TwoFactorUserIdCookie { get; set; }
}
