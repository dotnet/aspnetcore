// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.HostFiltering;

/// <summary>
/// A middleware used to filter requests by their Host header.
/// </summary>
public class HostFilteringMiddleware
{
    // Matches Http.Sys.
    private static readonly byte[] DefaultResponse = (
        "<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01//EN\"\"http://www.w3.org/TR/html4/strict.dtd\">\r\n"u8
            + "<HTML><HEAD><TITLE>Bad Request</TITLE>\r\n"u8
            + "<META HTTP-EQUIV=\"Content-Type\" Content=\"text/html; charset=us-ascii\"></ HEAD >\r\n"u8
            + "<BODY><h2>Bad Request - Invalid Hostname</h2>\r\n"u8
            + "<hr><p>HTTP Error 400. The request hostname is invalid.</p>\r\n"u8
            + "</BODY></HTML>"u8
        ).ToArray();

    private readonly RequestDelegate _next;
    private readonly ILogger<HostFilteringMiddleware> _logger;
    private readonly MiddlewareConfigurationManager _middlewareConfigurationManager;

    /// <summary>
    /// A middleware used to filter requests by their Host header.
    /// </summary>
    /// <param name="next"></param>
    /// <param name="logger"></param>
    /// <param name="optionsMonitor"></param>
    public HostFilteringMiddleware(
        RequestDelegate next,
        ILogger<HostFilteringMiddleware> logger,
        IOptionsMonitor<HostFilteringOptions> optionsMonitor
    )
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _middlewareConfigurationManager = new MiddlewareConfigurationManager(optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor)), logger);
    }

    /// <summary>
    /// Processes requests
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public Task Invoke(HttpContext context)
    {
        var middlewareConfiguration = _middlewareConfigurationManager.GetLatestMiddlewareConfiguration();

        if (!CheckHost(context, middlewareConfiguration))
        {
            return HostValidationFailed(context, middlewareConfiguration);
        }

        return _next(context);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Task HostValidationFailed(HttpContext context, MiddlewareConfiguration middlewareConfiguration)
    {
        context.Response.StatusCode = 400;

        if (middlewareConfiguration.IncludeFailureMessage)
        {
            context.Response.ContentLength = DefaultResponse.Length;
            context.Response.ContentType = "text/html";
            return context.Response.Body.WriteAsync(DefaultResponse, 0, DefaultResponse.Length);
        }

        return Task.CompletedTask;
    }

    // This does not duplicate format validations that are expected to be performed by the host.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool CheckHost(HttpContext context, MiddlewareConfiguration middlewareConfiguration)
    {
        var host = context.Request.Headers.Host.ToString();

        if (host.Length == 0)
        {
            return IsEmptyHostAllowed(context, middlewareConfiguration);
        }

        if (middlewareConfiguration.AllowAnyNonEmptyHost)
        {
            _logger.AllHostsAllowed();
            return true;
        }

        return CheckHostInAllowList(middlewareConfiguration.AllowedHosts, host);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool CheckHostInAllowList(IList<StringSegment>? allowedHosts, string host)
    {
        if (allowedHosts is not null && HostString.MatchesAny(new StringSegment(host), allowedHosts))
        {
            _logger.AllowedHostMatched(host);
            return true;
        }

        _logger.NoAllowedHostMatched(host);

        return false;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool IsEmptyHostAllowed(HttpContext context, MiddlewareConfiguration middlewareConfiguration)
    {
        // Http/1.0 does not require the host header.
        // Http/1.1 requires the header but the value may be empty.
        if (!middlewareConfiguration.AllowEmptyHosts)
        {
            _logger.RequestRejectedMissingHost(context.Request.Protocol);
            return false;
        }

        _logger.RequestAllowedMissingHost(context.Request.Protocol);

        return true;
    }
}
