// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.HttpOverrides;

/// <summary>
/// Middleware that converts a forward header into a client certificate if found.
/// </summary>
public class CertificateForwardingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly CertificateForwardingOptions _options;
    private readonly ILogger _logger;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="next"></param>
    /// <param name="loggerFactory"></param>
    /// <param name="options"></param>
    public CertificateForwardingMiddleware(
            RequestDelegate next,
            ILoggerFactory loggerFactory,
            IOptions<CertificateForwardingOptions> options)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(loggerFactory);
        ArgumentNullException.ThrowIfNull(options);

        _next = next;
        _options = options.Value;
        _logger = loggerFactory.CreateLogger<CertificateForwardingMiddleware>();
    }

    /// <summary>
    /// Looks for the presence of a <see cref="CertificateForwardingOptions.CertificateHeader"/> header in the request,
    /// if found, converts this header to a ClientCertificate set on the connection.
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/>.</param>
    /// <returns>A <see cref="Task"/>.</returns>
    public Task Invoke(HttpContext httpContext)
    {
        var header = httpContext.Request.Headers[_options.CertificateHeader];
        if (!StringValues.IsNullOrEmpty(header))
        {
            httpContext.Features.Set<ITlsConnectionFeature>(new CertificateForwardingFeature(_logger, header, _options));
        }
        return _next(httpContext);
    }
}
