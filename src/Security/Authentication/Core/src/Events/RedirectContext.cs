// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// Context passed for redirect events.
/// </summary>
public class RedirectContext<TOptions> : PropertiesContext<TOptions> where TOptions : AuthenticationSchemeOptions
{
    /// <summary>
    /// Creates a new context object.
    /// </summary>
    /// <param name="context">The HTTP request context</param>
    /// <param name="scheme">The scheme data</param>
    /// <param name="options">The handler options</param>
    /// <param name="redirectUri">The initial redirect URI</param>
    /// <param name="properties">The <see cref="AuthenticationProperties"/>.</param>
    public RedirectContext(
        HttpContext context,
        AuthenticationScheme scheme,
        TOptions options,
        AuthenticationProperties properties,
        string redirectUri)
        : base(context, scheme, options, properties)
    {
        Properties = properties;
        RedirectUri = redirectUri;
    }

    /// <summary>
    /// Gets or Sets the URI used for the redirect operation.
    /// </summary>
    public string RedirectUri { get; set; }
}
