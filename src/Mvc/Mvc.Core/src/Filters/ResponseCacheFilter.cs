// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Filters;

/// <summary>
/// An <see cref="IActionFilter"/> which sets the appropriate headers related to response caching.
/// </summary>
internal partial class ResponseCacheFilter : IActionFilter, IResponseCacheFilter
{
    private readonly ResponseCacheFilterExecutor _executor;
    private readonly ILogger _logger;

    /// <summary>
    /// Creates a new instance of <see cref="ResponseCacheFilter"/>
    /// </summary>
    /// <param name="cacheProfile">The profile which contains the settings for
    /// <see cref="ResponseCacheFilter"/>.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    public ResponseCacheFilter(CacheProfile cacheProfile, ILoggerFactory loggerFactory)
    {
        _executor = new ResponseCacheFilterExecutor(cacheProfile);
        _logger = loggerFactory.CreateLogger(GetType());
    }

    /// <summary>
    /// Gets or sets the duration in seconds for which the response is cached.
    /// This is a required parameter.
    /// This sets "max-age" in "Cache-control" header.
    /// </summary>
    public int Duration
    {
        get => _executor.Duration;
        set => _executor.Duration = value;
    }

    /// <summary>
    /// Gets or sets the location where the data from a particular URL must be cached.
    /// </summary>
    public ResponseCacheLocation Location
    {
        get => _executor.Location;
        set => _executor.Location = value;
    }

    /// <summary>
    /// Gets or sets the value which determines whether the data should be stored or not.
    /// When set to <see langword="true"/>, it sets "Cache-control" header to "no-store".
    /// Ignores the "Location" parameter for values other than "None".
    /// Ignores the "duration" parameter.
    /// </summary>
    public bool NoStore
    {
        get => _executor.NoStore;
        set => _executor.NoStore = value;
    }

    /// <summary>
    /// Gets or sets the value for the Vary response header.
    /// </summary>
    public string? VaryByHeader
    {
        get => _executor.VaryByHeader;
        set => _executor.VaryByHeader = value;
    }

    /// <summary>
    /// Gets or sets the query keys to vary by.
    /// </summary>
    /// <remarks>
    /// <see cref="VaryByQueryKeys"/> requires the response cache middleware.
    /// </remarks>
    public string[]? VaryByQueryKeys
    {
        get => _executor.VaryByQueryKeys;
        set => _executor.VaryByQueryKeys = value;
    }

    /// <inheritdoc />
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        // If there are more filters which can override the values written by this filter,
        // then skip execution of this filter.
        var effectivePolicy = context.FindEffectivePolicy<IResponseCacheFilter>();
        if (effectivePolicy != null && effectivePolicy != this)
        {
            Log.NotMostEffectiveFilter(_logger, GetType(), effectivePolicy.GetType(), typeof(IResponseCacheFilter));
            return;
        }

        _executor.Execute(context);
    }

    /// <inheritdoc />
    public void OnActionExecuted(ActionExecutedContext context)
    {
    }

    private static partial class Log
    {
        [LoggerMessage(4, LogLevel.Debug, "Execution of filter {OverriddenFilter} is preempted by filter {OverridingFilter} which is the most effective filter implementing policy {FilterPolicy}.", EventName = "NotMostEffectiveFilter")]
        public static partial void NotMostEffectiveFilter(ILogger logger, Type overriddenFilter, Type overridingFilter, Type filterPolicy);
    }
}
