// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Provides programmatic configuration for the <see cref="CookiePolicyMiddleware"/>.
/// </summary>
public class CookiePolicyOptions
{
    private string _consentCookieValue = "yes";

    /// <summary>
    /// Affects the cookie's same site attribute.
    /// </summary>
    public SameSiteMode MinimumSameSitePolicy { get; set; } = SameSiteMode.Unspecified;

    /// <summary>
    /// Affects whether cookies must be HttpOnly.
    /// </summary>
    public HttpOnlyPolicy HttpOnly { get; set; } = HttpOnlyPolicy.None;

    /// <summary>
    /// Affects whether cookies must be Secure.
    /// </summary>
    public CookieSecurePolicy Secure { get; set; } = CookieSecurePolicy.None;

    /// <summary>
    /// Gets or sets the <see cref="CookieBuilder"/> that is used to track if the user consented to the
    /// cookie use policy.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If an explicit <see cref="CookieBuilder.Name"/> is not provided, the system will automatically generate a
    /// unique name that begins with <c>.AspNet.Consent</c>.
    /// </para>
    /// <list type="bullet">
    /// <item><description><see cref="CookieBuilder.IsEssential"/> defaults to <c>true</c>.</description></item>
    /// <item><description><see cref="CookieBuilder.Expiration"/> defaults to 365 days.</description></item>
    /// </list>
    /// </remarks>
    public CookieBuilder ConsentCookie { get; set; } = new CookieBuilder()
    {
        Name = ".AspNet.Consent",
        Expiration = TimeSpan.FromDays(365),
        IsEssential = true,
    };

    /// <summary>
    /// Gets or sets the value for the cookie used to track if the user consented to the
    /// cookie use policy.
    /// </summary>
    /// <value>Defaults to <c>yes</c>.</value>
    public string ConsentCookieValue
    {
        get => _consentCookieValue;
        set
        {
            ArgumentException.ThrowIfNullOrEmpty(value);
            _consentCookieValue = value;
        }
    }

    /// <summary>
    /// Checks if consent policies should be evaluated on this request. The default is false.
    /// </summary>
    public Func<HttpContext, bool>? CheckConsentNeeded { get; set; }

    /// <summary>
    /// Called when a cookie is appended.
    /// </summary>
    public Action<AppendCookieContext>? OnAppendCookie { get; set; }

    /// <summary>
    /// Called when a cookie is deleted.
    /// </summary>
    public Action<DeleteCookieContext>? OnDeleteCookie { get; set; }
}
