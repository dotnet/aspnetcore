// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Filters;

/// <summary>
/// A filter that sets <see cref="IHttpMaxRequestBodySizeFeature.MaxRequestBodySize"/>
/// to <c>null</c>.
/// </summary>
internal partial class DisableRequestSizeLimitFilter : IAuthorizationFilter, IRequestSizePolicy
{
    private readonly ILogger _logger;

    /// <summary>
    /// Creates a new instance of <see cref="DisableRequestSizeLimitFilter"/>.
    /// </summary>
    public DisableRequestSizeLimitFilter(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<DisableRequestSizeLimitFilter>();
    }

    /// <summary>
    /// Sets the <see cref="IHttpMaxRequestBodySizeFeature.MaxRequestBodySize"/>
    /// to <c>null</c>.
    /// </summary>
    /// <param name="context">The <see cref="AuthorizationFilterContext"/>.</param>
    /// <remarks>If <see cref="IHttpMaxRequestBodySizeFeature"/> is not enabled or is read-only,
    /// the <see cref="DisableRequestSizeLimitAttribute"/> is not applied.</remarks>
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var effectivePolicy = context.FindEffectivePolicy<IRequestSizePolicy>();
        if (effectivePolicy != null && effectivePolicy != this)
        {
            Log.NotMostEffectiveFilter(_logger, GetType(), effectivePolicy.GetType(), typeof(IRequestSizePolicy));
            return;
        }

        var maxRequestBodySizeFeature = context.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();

        if (maxRequestBodySizeFeature == null)
        {
            Log.FeatureNotFound(_logger);
        }
        else if (maxRequestBodySizeFeature.IsReadOnly)
        {
            Log.FeatureIsReadOnly(_logger);
        }
        else
        {
            maxRequestBodySizeFeature.MaxRequestBodySize = null;
            Log.RequestBodySizeLimitDisabled(_logger);
        }
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Warning, "A request body size limit could not be applied. This server does not support the IHttpRequestBodySizeFeature.", EventName = "FeatureNotFound")]
        public static partial void FeatureNotFound(ILogger logger);

        [LoggerMessage(2, LogLevel.Warning, "A request body size limit could not be applied. The IHttpRequestBodySizeFeature for the server is read-only.", EventName = "FeatureIsReadOnly")]
        public static partial void FeatureIsReadOnly(ILogger logger);

        [LoggerMessage(3, LogLevel.Debug, "The request body size limit has been disabled.", EventName = "RequestBodySizeLimitDisabled")]
        public static partial void RequestBodySizeLimitDisabled(ILogger logger);

        [LoggerMessage(4, LogLevel.Debug, "Execution of filter {OverriddenFilter} is preempted by filter {OverridingFilter} which is the most effective filter implementing policy {FilterPolicy}.", EventName = "NotMostEffectiveFilter")]
        public static partial void NotMostEffectiveFilter(ILogger logger, Type overriddenFilter, Type overridingFilter, Type filterPolicy);
    }
}
