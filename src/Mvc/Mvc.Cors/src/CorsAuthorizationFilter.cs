// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.Cors;

/// <summary>
/// A filter that applies the given <see cref="CorsPolicy"/> and adds appropriate response headers.
/// </summary>
public class CorsAuthorizationFilter : ICorsAuthorizationFilter
{
    private readonly ICorsService _corsService;
    private readonly ICorsPolicyProvider _corsPolicyProvider;
    private readonly ILogger _logger;

    /// <summary>
    /// Creates a new instance of <see cref="CorsAuthorizationFilter"/>.
    /// </summary>
    /// <param name="corsService">The <see cref="ICorsService"/>.</param>
    /// <param name="policyProvider">The <see cref="ICorsPolicyProvider"/>.</param>
    public CorsAuthorizationFilter(ICorsService corsService, ICorsPolicyProvider policyProvider)
        : this(corsService, policyProvider, NullLoggerFactory.Instance)
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="CorsAuthorizationFilter"/>.
    /// </summary>
    /// <param name="corsService">The <see cref="ICorsService"/>.</param>
    /// <param name="policyProvider">The <see cref="ICorsPolicyProvider"/>.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    public CorsAuthorizationFilter(
        ICorsService corsService,
        ICorsPolicyProvider policyProvider,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(corsService);
        ArgumentNullException.ThrowIfNull(policyProvider);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _corsService = corsService;
        _corsPolicyProvider = policyProvider;
        _logger = loggerFactory.CreateLogger(GetType());
    }

    /// <summary>
    /// The policy name used to fetch a <see cref="CorsPolicy"/>.
    /// </summary>
    public string? PolicyName { get; set; }

    /// <inheritdoc />
    // Since clients' preflight requests would not have data to authenticate requests, this
    // filter must run before any other authorization filters.
    public int Order => int.MinValue + 100;

    /// <inheritdoc />
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // If this filter is not closest to the action, it is not applicable.
        if (!context.IsEffectivePolicy<ICorsAuthorizationFilter>(this))
        {
            _logger.NotMostEffectiveFilter(typeof(ICorsAuthorizationFilter));
            return;
        }

        var httpContext = context.HttpContext;
        var request = httpContext.Request;
        if (request.Headers.ContainsKey(CorsConstants.Origin))
        {
            var policy = await _corsPolicyProvider.GetPolicyAsync(httpContext, PolicyName);

            if (policy == null)
            {
                throw new InvalidOperationException(
                    Resources.FormatCorsAuthorizationFilter_MissingCorsPolicy(PolicyName));
            }

            var result = _corsService.EvaluatePolicy(context.HttpContext, policy);
            _corsService.ApplyResult(result, context.HttpContext.Response);

            var accessControlRequestMethod =
                    httpContext.Request.Headers[CorsConstants.AccessControlRequestMethod];
            if (HttpMethods.IsOptions(request.Method)
                && !StringValues.IsNullOrEmpty(accessControlRequestMethod))
            {
                // If this was a preflight, there is no need to run anything else.
                context.Result = new StatusCodeResult(StatusCodes.Status204NoContent);
            }

            // Continue with other filters and action.
        }
    }
}
