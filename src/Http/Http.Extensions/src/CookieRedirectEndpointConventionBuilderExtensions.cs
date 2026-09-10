// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Cookie redirect extension methods for <see cref="IEndpointConventionBuilder"/>.
/// </summary>
public static class CookieRedirectEndpointConventionBuilderExtensions
{
    private static readonly AllowCookieRedirectAttribute _allowCookieRedirectAttribute = new();

    /// <summary>
    /// Specifies that cookie-based authentication redirects are disabled for an endpoint using <see cref="IDisableCookieRedirectMetadata"/>.
    /// When present and not overridden by <see cref="AllowCookieRedirect"/> or <see cref="IAllowCookieRedirectMetadata"/>,
    /// the cookie authentication handler will prefer using 401 and 403 status codes over redirecting to the login or access denied paths.
    /// </summary>
    /// <typeparam name="TBuilder">The type of endpoint convention builder.</typeparam>
    /// <param name="builder">The endpoint convention builder.</param>
    /// <returns>The original convention builder parameter.</returns>
    public static TBuilder DisableCookieRedirect<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
    {
        builder.Add(b => b.Metadata.Add(DisableCookieRedirectMetadata.Instance));
        return builder;
    }

    /// <summary>
    /// Specifies that cookie-based authentication redirects are allowed for an endpoint using <see cref="IAllowCookieRedirectMetadata"/>.
    /// This is normally the default behavior, but it exists to override <see cref="IDisableCookieRedirectMetadata"/> no matter the order.
    /// When present, the cookie authentication handler will prefer browser login or access denied redirects over 401 and 403 status codes.
    /// </summary>
    /// <typeparam name="TBuilder">The type of endpoint convention builder.</typeparam>
    /// <param name="builder">The endpoint convention builder.</param>
    /// <returns>The original convention builder parameter.</returns>
    public static TBuilder AllowCookieRedirect<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
    {
        builder.Add(b => b.Metadata.Add(_allowCookieRedirectAttribute));
        return builder;
    }
}
