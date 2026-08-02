// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.CookiePolicy;

/// <summary>
/// Initializes a new instance of <see cref="CookiePolicyMiddleware"/>.
/// </summary>
/// <remarks>
/// When using <see cref="CookieOptions"/> to configure cookies, note that a
/// <see cref="CookieOptions"/> instance is intended to govern the behavior of an individual cookie.
/// Reusing the same <see cref="CookieOptions"/> instance across multiple cookies can lead to unintended
/// consequences, such as modifications affecting multiple cookies. We recommend instantiating a new
/// <see cref="CookieOptions"/> object for each cookie to ensure that the configuration is applied
/// independently.
/// </remarks>
public class CookiePolicyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="CookiePolicyMiddleware"/>.
    /// </summary>
    /// <param name="next">A reference to the next item in the application pipeline.</param>
    /// <param name="options">Accessor to <see cref="CookiePolicyOptions"/>.</param>
    /// <param name="factory">The <see cref="ILoggerFactory"/>.</param>
    public CookiePolicyMiddleware(RequestDelegate next, IOptions<CookiePolicyOptions> options, ILoggerFactory factory)
    {
        Options = options.Value;
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = factory.CreateLogger<CookiePolicyMiddleware>();
    }

    /// <summary>
    /// Initializes a new instance of <see cref="CookiePolicyMiddleware"/>.
    /// </summary>
    /// <param name="next">A reference to the next item in the application pipeline.</param>
    /// <param name="options">Accessor to <see cref="CookiePolicyOptions"/>.</param>
    public CookiePolicyMiddleware(RequestDelegate next, IOptions<CookiePolicyOptions> options)
    {
        Options = options.Value;
        _next = next;
        _logger = NullLogger.Instance;
    }

    /// <summary>
    /// Gets or sets the <see cref="CookiePolicyOptions"/>.
    /// </summary>
    public CookiePolicyOptions Options { get; set; }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext" />.</param>
    public Task Invoke(HttpContext context)
    {
        var feature = context.Features.Get<IResponseCookiesFeature>() ?? new ResponseCookiesFeature(context.Features);
        var wrapper = new ResponseCookiesWrapper(context, Options, feature, _logger);
        context.Features.Set<IResponseCookiesFeature>(new CookiesWrapperFeature(wrapper));
        context.Features.Set<ITrackingConsentFeature>(wrapper);

        return _next(context);
    }

    private sealed class CookiesWrapperFeature : IResponseCookiesFeature
    {
        public CookiesWrapperFeature(ResponseCookiesWrapper wrapper)
        {
            Cookies = wrapper;
        }

        public IResponseCookies Cookies { get; }
    }
}
