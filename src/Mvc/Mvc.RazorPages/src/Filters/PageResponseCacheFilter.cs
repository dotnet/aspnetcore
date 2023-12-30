// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Filters;

/// <summary>
/// A <see cref="IPageFilter"/> which sets the appropriate headers related to response caching.
/// </summary>
internal sealed class PageResponseCacheFilter : IPageFilter, IResponseCacheFilter
{
    private readonly ResponseCacheFilterExecutor _executor;
    private readonly ILogger _logger;

    /// <summary>
    /// Creates a new instance of <see cref="PageResponseCacheFilter"/>
    /// </summary>
    /// <param name="cacheProfile">The profile which contains the settings for
    /// <see cref="PageResponseCacheFilter"/>.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    public PageResponseCacheFilter(CacheProfile cacheProfile, ILoggerFactory loggerFactory)
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

    public void OnPageHandlerSelected(PageHandlerSelectedContext context)
    {
    }

    public void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.IsEffectivePolicy<IResponseCacheFilter>(this))
        {
            _logger.NotMostEffectiveFilter(typeof(IResponseCacheFilter));
            return;
        }

        _executor.Execute(context);
    }

    public void OnPageHandlerExecuted(PageHandlerExecutedContext context)
    {
    }
}
