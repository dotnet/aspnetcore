// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Antiforgery;

/// <summary>
/// Provides programmatic configuration for the antiforgery token system.
/// </summary>
public class AntiforgeryOptions
{
    private const string AntiforgeryTokenFieldName = "__RequestVerificationToken";
    private const string AntiforgeryTokenHeaderName = "RequestVerificationToken";

    private string _formFieldName = AntiforgeryTokenFieldName;

    private CookieBuilder _cookieBuilder = new CookieBuilder
    {
        SameSite = SameSiteMode.Strict,
        HttpOnly = true,

        // Check the comment on CookieBuilder for more details
        IsEssential = true,

        // Some browsers do not allow non-secure endpoints to set cookies with a 'secure' flag or overwrite cookies
        // whose 'secure' flag is set (http://httpwg.org/http-extensions/draft-ietf-httpbis-cookie-alone.html).
        // Since mixing secure and non-secure endpoints is a common scenario in applications, we are relaxing the
        // restriction on secure policy on some cookies by setting to 'None'. Cookies related to authentication or
        // authorization use a stronger policy than 'None'.
        SecurePolicy = CookieSecurePolicy.None,
    };

    /// <summary>
    /// The default cookie prefix, which is ".AspNetCore.Antiforgery.".
    /// </summary>
    public static readonly string DefaultCookiePrefix = ".AspNetCore.Antiforgery.";

    /// <summary>
    /// Determines the settings used to create the antiforgery cookies.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If an explicit <see cref="CookieBuilder.Name"/> is not provided, the system will automatically generate a
    /// unique name that begins with <see cref="DefaultCookiePrefix"/>.
    /// </para>
    /// <list type="bullet">
    /// <item><description><see cref="CookieBuilder.SameSite"/> defaults to <see cref="SameSiteMode.Strict"/>.</description></item>
    /// <item><description><see cref="CookieBuilder.HttpOnly"/> defaults to <c>true</c>.</description></item>
    /// <item><description><see cref="CookieBuilder.IsEssential"/> defaults to <c>true</c>. The cookie used by the antiforgery system
    /// is part of a security system that is necessary when using cookie-based authentication. It should be
    /// considered required for the application to function.</description></item>
    /// <item><description><see cref="CookieBuilder.SecurePolicy"/> defaults to <see cref="CookieSecurePolicy.None"/>.</description></item>
    /// </list>
    /// </remarks>
    public CookieBuilder Cookie
    {
        get => _cookieBuilder;
        set => _cookieBuilder = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Specifies the name of the antiforgery token field that is used by the antiforgery system.
    /// </summary>
    public string FormFieldName
    {
        get => _formFieldName;
        set => _formFieldName = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Specifies the name of the header value that is used by the antiforgery system. If <c>null</c> then
    /// antiforgery validation will only consider form data.
    /// </summary>
    public string? HeaderName { get; set; } = AntiforgeryTokenHeaderName;

    /// <summary>
    /// Specifies whether to suppress the generation of X-Frame-Options header
    /// which is used to prevent ClickJacking. By default, the X-Frame-Options
    /// header is generated with the value SAMEORIGIN. If this setting is 'true',
    /// the X-Frame-Options header will not be generated for the response.
    /// </summary>
    public bool SuppressXFrameOptionsHeader { get; set; }

    /// <summary>
    /// Specifies whether to suppress load of antiforgery token from request body.
    /// </summary>
    public bool SuppressReadingTokenFromFormBody { get; set; }
}
