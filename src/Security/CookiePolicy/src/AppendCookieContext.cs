// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.CookiePolicy;

/// <summary>
/// Context for <see cref="CookiePolicyOptions.OnAppendCookie"/> that allows changes to the cookie prior to being appended.
/// </summary>
public class AppendCookieContext
{
    /// <summary>
    /// Initializes a new instance of <see cref="AppendCookieContext"/>.
    /// </summary>
    /// <param name="context">The request <see cref="HttpContext"/>.</param>
    /// <param name="options">The <see cref="Http.CookieOptions"/> passed to the cookie policy.</param>
    /// <param name="name">The cookie name.</param>
    /// <param name="value">The cookie value.</param>
    public AppendCookieContext(HttpContext context, CookieOptions options, string name, string value)
    {
        Context = context;
        CookieOptions = options;
        CookieName = name;
        CookieValue = value;
    }

    /// <summary>
    /// Gets the <see cref="HttpContext"/>.
    /// </summary>
    public HttpContext Context { get; }

    /// <summary>
    /// Gets the <see cref="Http.CookieOptions"/>.
    /// </summary>
    public CookieOptions CookieOptions { get; }

    /// <summary>
    /// Gets or sets the cookie name.
    /// </summary>
    public string CookieName { get; set; }

    /// <summary>
    /// Gets or sets the cookie value.
    /// </summary>
    public string CookieValue { get; set; }

    /// <summary>
    /// Gets a value that determines if cookie consent is required before setting this cookie.
    /// </summary>
    public bool IsConsentNeeded { get; internal set; }

    /// <summary>
    /// Gets a value that determines if cookie consent was provided.
    /// </summary>
    public bool HasConsent { get; internal set; }

    /// <summary>
    /// Gets or sets a value that determines if the cookie can be appended. If set to <see langword="false" />,
    /// the cookie is not appended.
    /// </summary>
    public bool IssueCookie { get; set; }
}
