// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Components.Endpoints;

/// <summary>
/// Provides programmatic configuration for cookies set by <see cref="CookieTempDataProvider"/>
/// </summary>
public class CookieTempDataProviderOptions
{
    private CookieBuilder _cookieBuilder = new CookieBuilder
    {
        Name = CookieTempDataProvider.CookieName,
        HttpOnly = true,
        SameSite = SameSiteMode.Lax,
        IsEssential = false,
        SecurePolicy = CookieSecurePolicy.SameAsRequest,
    };

    /// <summary>
    /// Determines the settings used to create the cookie in <see cref="CookieTempDataProvider"/>.
    /// </summary>
    public CookieBuilder Cookie
    {
        get => _cookieBuilder;
        set => _cookieBuilder = value ?? throw new ArgumentNullException(nameof(value));
    }
}
