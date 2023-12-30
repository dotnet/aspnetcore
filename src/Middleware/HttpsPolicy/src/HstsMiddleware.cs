// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.HttpsPolicy;

/// <summary>
/// Enables HTTP Strict Transport Security (HSTS)
/// See <see href="https://tools.ietf.org/html/rfc6797"/>.
/// </summary>
public class HstsMiddleware
{
    private const string IncludeSubDomains = "; includeSubDomains";
    private const string Preload = "; preload";

    private readonly RequestDelegate _next;
    private readonly StringValues _strictTransportSecurityValue;
    private readonly IList<string> _excludedHosts;
    private readonly ILogger _logger;

    /// <summary>
    /// Initialize the HSTS middleware.
    /// </summary>
    /// <param name="next"></param>
    /// <param name="options"></param>
    /// <param name="loggerFactory"></param>
    public HstsMiddleware(RequestDelegate next, IOptions<HstsOptions> options, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(options);

        _next = next ?? throw new ArgumentNullException(nameof(next));

        var hstsOptions = options.Value;
        var maxAge = Convert.ToInt64(Math.Floor(hstsOptions.MaxAge.TotalSeconds))
                        .ToString(CultureInfo.InvariantCulture);
        var includeSubdomains = hstsOptions.IncludeSubDomains ? IncludeSubDomains : StringSegment.Empty;
        var preload = hstsOptions.Preload ? Preload : StringSegment.Empty;
        _strictTransportSecurityValue = new StringValues($"max-age={maxAge}{includeSubdomains}{preload}");
        _excludedHosts = hstsOptions.ExcludedHosts;
        _logger = loggerFactory.CreateLogger<HstsMiddleware>();
    }

    /// <summary>
    /// Initialize the HSTS middleware.
    /// </summary>
    /// <param name="next"></param>
    /// <param name="options"></param>
    public HstsMiddleware(RequestDelegate next, IOptions<HstsOptions> options)
        : this(next, options, NullLoggerFactory.Instance) { }

    /// <summary>
    /// Invoke the middleware.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <returns></returns>
    public Task Invoke(HttpContext context)
    {
        if (!context.Request.IsHttps)
        {
            _logger.SkippingInsecure();
            return _next(context);
        }

        if (IsHostExcluded(context.Request.Host.Host))
        {
            _logger.SkippingExcludedHost(context.Request.Host.Host);
            return _next(context);
        }

        context.Response.Headers.StrictTransportSecurity = _strictTransportSecurityValue;
        _logger.AddingHstsHeader();

        return _next(context);
    }

    private bool IsHostExcluded(string host)
    {
        for (var i = 0; i < _excludedHosts.Count; i++)
        {
            if (string.Equals(host, _excludedHosts[i], StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
