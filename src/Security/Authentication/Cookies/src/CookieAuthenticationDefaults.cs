// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.Cookies;

/// <summary>
/// Default values related to cookie-based authentication handler
/// </summary>
public static class CookieAuthenticationDefaults
{
    /// <summary>
    /// The default value used for CookieAuthenticationOptions.AuthenticationScheme
    /// </summary>
    public const string AuthenticationScheme = "Cookies";

    /// <summary>
    /// The prefix used to provide a default CookieAuthenticationOptions.CookieName
    /// </summary>
    public static readonly string CookiePrefix = ".AspNetCore.";

    /// <summary>
    /// The default value used by CookieAuthenticationMiddleware for the
    /// CookieAuthenticationOptions.LoginPath
    /// </summary>
    public static readonly PathString LoginPath = new PathString("/Account/Login");

    /// <summary>
    /// The default value used by CookieAuthenticationMiddleware for the
    /// CookieAuthenticationOptions.LogoutPath
    /// </summary>
    public static readonly PathString LogoutPath = new PathString("/Account/Logout");

    /// <summary>
    /// The default value used by CookieAuthenticationMiddleware for the
    /// CookieAuthenticationOptions.AccessDeniedPath
    /// </summary>
    public static readonly PathString AccessDeniedPath = new PathString("/Account/AccessDenied");

    /// <summary>
    /// The default value of the CookieAuthenticationOptions.ReturnUrlParameter
    /// </summary>
    public static readonly string ReturnUrlParameter = "ReturnUrl";
}
