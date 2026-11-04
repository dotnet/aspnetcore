// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Mvc.TagHelpers;

/// <summary>
/// <see cref="TagHelper"/> base implementation for caching elements.
/// </summary>
public abstract class CacheTagHelperBase : TagHelper
{
    private const string VaryByAttributeName = "vary-by";
    private const string VaryByHeaderAttributeName = "vary-by-header";
    private const string VaryByQueryAttributeName = "vary-by-query";
    private const string VaryByRouteAttributeName = "vary-by-route";
    private const string VaryByCookieAttributeName = "vary-by-cookie";
    private const string VaryByUserAttributeName = "vary-by-user";
    private const string VaryByCultureAttributeName = "vary-by-culture";
    private const string ExpiresOnAttributeName = "expires-on";
    private const string ExpiresAfterAttributeName = "expires-after";
    private const string ExpiresSlidingAttributeName = "expires-sliding";
    private const string EnabledAttributeName = "enabled";

    /// <summary>
    /// The default duration, from the time the cache entry was added, when it should be evicted.
    /// This default duration will only be used if no other expiration criteria is specified.
    /// The default expiration time is a sliding expiration of 30 seconds.
    /// </summary>
    public static readonly TimeSpan DefaultExpiration = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Creates a new <see cref="CacheTagHelperBase"/>.
    /// </summary>
    /// <param name="htmlEncoder">The <see cref="HtmlEncoder"/> to use.</param>
    public CacheTagHelperBase(HtmlEncoder htmlEncoder)
    {
        HtmlEncoder = htmlEncoder;
    }

    /// <inheritdoc />
    public override int Order => -1000;

    /// <summary>
    /// Gets the <see cref="System.Text.Encodings.Web.HtmlEncoder"/> which encodes the content to be cached.
    /// </summary>
    protected HtmlEncoder HtmlEncoder { get; }

    /// <summary>
    /// Gets or sets the <see cref="ViewContext"/> for the current executing View.
    /// </summary>
    [HtmlAttributeNotBound]
    [ViewContext]
    public ViewContext ViewContext { get; set; }

    /// <summary>
    /// Gets or sets a <see cref="string" /> to vary the cached result by.
    /// </summary>
    [HtmlAttributeName(VaryByAttributeName)]
    public string VaryBy { get; set; }

    /// <summary>
    /// Gets or sets a comma-delimited set of HTTP request headers to vary the cached result by.
    /// </summary>
    [HtmlAttributeName(VaryByHeaderAttributeName)]
    public string VaryByHeader { get; set; }

    /// <summary>
    /// Gets or sets a comma-delimited set of query parameters to vary the cached result by.
    /// </summary>
    [HtmlAttributeName(VaryByQueryAttributeName)]
    public string VaryByQuery { get; set; }

    /// <summary>
    /// Gets or sets a comma-delimited set of route data parameters to vary the cached result by.
    /// </summary>
    [HtmlAttributeName(VaryByRouteAttributeName)]
    public string VaryByRoute { get; set; }

    /// <summary>
    /// Gets or sets a comma-delimited set of cookie names to vary the cached result by.
    /// </summary>
    [HtmlAttributeName(VaryByCookieAttributeName)]
    public string VaryByCookie { get; set; }

    /// <summary>
    /// Gets or sets a value that determines if the cached result is to be varied by the Identity for the logged in
    /// <see cref="Http.HttpContext.User"/>.
    /// </summary>
    [HtmlAttributeName(VaryByUserAttributeName)]
    public bool VaryByUser { get; set; }

    /// <summary>
    /// Gets or sets a value that determines if the cached result is to be varied by request culture.
    /// <para>
    /// Setting this to <c>true</c> would result in the result to be varied by <see cref="CultureInfo.CurrentCulture" />
    /// and <see cref="CultureInfo.CurrentUICulture" />.
    /// </para>
    /// </summary>
    [HtmlAttributeName(VaryByCultureAttributeName)]
    public bool VaryByCulture { get; set; }

    /// <summary>
    /// Gets or sets the exact <see cref="DateTimeOffset"/> the cache entry should be evicted.
    /// </summary>
    [HtmlAttributeName(ExpiresOnAttributeName)]
    public DateTimeOffset? ExpiresOn { get; set; }

    /// <summary>
    /// Gets or sets the duration, from the time the cache entry was added, when it should be evicted.
    /// </summary>
    [HtmlAttributeName(ExpiresAfterAttributeName)]
    public TimeSpan? ExpiresAfter { get; set; }

    /// <summary>
    /// Gets or sets the duration from last access that the cache entry should be evicted.
    /// </summary>
    [HtmlAttributeName(ExpiresSlidingAttributeName)]
    public TimeSpan? ExpiresSliding { get; set; }

    /// <summary>
    /// Gets or sets the value which determines if the tag helper is enabled or not.
    /// </summary>
    [HtmlAttributeName(EnabledAttributeName)]
    public bool Enabled { get; set; } = true;
}
