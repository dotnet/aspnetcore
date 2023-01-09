// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Rewrite.Logging;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Rewrite;

/// <summary>
/// Represents a middleware that rewrites urls
/// </summary>
public class RewriteMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RewriteOptions _options;
    private readonly IFileProvider _fileProvider;
    private readonly ILogger _logger;

    /// <summary>
    /// Creates a new instance of <see cref="RewriteMiddleware"/>
    /// </summary>
    /// <param name="next">The delegate representing the next middleware in the request pipeline.</param>
    /// <param name="hostingEnvironment">The Hosting Environment.</param>
    /// <param name="loggerFactory">The Logger Factory.</param>
    /// <param name="options">The middleware options, containing the rules to apply.</param>
    public RewriteMiddleware(
        RequestDelegate next,
        IWebHostEnvironment hostingEnvironment,
        ILoggerFactory loggerFactory,
        IOptions<RewriteOptions> options)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(options);

        _next = next;
        _options = options.Value;
        _fileProvider = _options.StaticFileProvider ?? hostingEnvironment.WebRootFileProvider;
        _logger = loggerFactory.CreateLogger<RewriteMiddleware>();
    }

    /// <summary>
    /// Executes the middleware.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> for the current request.</param>
    /// <returns>A task that represents the execution of this middleware.</returns>
    public Task Invoke(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var rewriteContext = new RewriteContext
        {
            HttpContext = context,
            StaticFileProvider = _fileProvider,
            Logger = _logger,
            Result = RuleResult.ContinueRules
        };

        var originalPath = context.Request.Path;

        RunRules(rewriteContext, _options, context, _logger);
        if (rewriteContext.Result == RuleResult.EndResponse)
        {
            return Task.CompletedTask;
        }

        // If a rule changed the path we want routing to find a new endpoint
        if (originalPath != context.Request.Path)
        {
            if (_options.BranchedNext is not null)
            {
                // An endpoint may have already been set. Since we're going to re-invoke the middleware pipeline we need to reset
                // the endpoint and route values to ensure things are re-calculated.
                context.SetEndpoint(endpoint: null);
                var routeValuesFeature = context.Features.Get<IRouteValuesFeature>();
                if (routeValuesFeature is not null)
                {
                    routeValuesFeature.RouteValues = null!;
                }
                return _options.BranchedNext(context);
            }
        }

        return _next(context);
    }

    static void RunRules(RewriteContext rewriteContext, RewriteOptions options, HttpContext httpContext, ILogger logger)
    {
        foreach (var rule in options.Rules)
        {
            rule.ApplyRule(rewriteContext);
            switch (rewriteContext.Result)
            {
                case RuleResult.ContinueRules:
                    logger.RewriteMiddlewareRequestContinueResults(httpContext.Request.GetEncodedUrl());
                    break;
                case RuleResult.EndResponse:
                    logger.RewriteMiddlewareRequestResponseComplete(
                        httpContext.Response.Headers.Location.ToString(),
                        httpContext.Response.StatusCode);
                    return;
                case RuleResult.SkipRemainingRules:
                    logger.RewriteMiddlewareRequestStopRules(httpContext.Request.GetEncodedUrl());
                    return;
                default:
                    throw new ArgumentOutOfRangeException($"Invalid rule termination {rewriteContext.Result}");
            }
        }
    }
}
