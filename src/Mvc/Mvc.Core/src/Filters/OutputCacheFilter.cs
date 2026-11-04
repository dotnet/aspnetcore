// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Filters;

/// <summary>
/// An <see cref="IActionFilter"/> which sets the appropriate headers related to output caching.
/// </summary>
internal partial class OutputCacheFilter : IActionFilter
{
    private readonly ILogger _logger;

    /// <summary>
    /// Creates a new instance of <see cref="OutputCacheFilter"/>
    /// </summary>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    public OutputCacheFilter(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger(GetType());
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // If there are more filters which can override the values written by this filter,
        // then skip execution of this filter.
        var effectivePolicy = context.FindEffectivePolicy<IOutputCacheFilter>();
        if (effectivePolicy != null && effectivePolicy != this)
        {
            Log.NotMostEffectiveFilter(_logger, GetType(), effectivePolicy.GetType(), typeof(IOutputCacheFilter));
            return;
        }

        var outputCachingFeature = context.HttpContext.Features.Get<IOutputCacheFeature>();
        if (outputCachingFeature == null)
        {
            throw new InvalidOperationException(
                Resources.FormatOutputCacheAttribute_Requires_OutputCachingMiddleware(nameof(OutputCacheAttribute)));
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, "Execution of filter {OverriddenFilter} is preempted by filter {OverridingFilter} which is the most effective filter implementing policy {FilterPolicy}.", EventName = "NotMostEffectiveFilter")]
        public static partial void NotMostEffectiveFilter(ILogger logger, Type overriddenFilter, Type overridingFilter, Type filterPolicy);
    }
}
