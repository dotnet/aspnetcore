// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

internal sealed class ResponseCacheFilterApplicationModelProvider : IPageApplicationModelProvider
{
    private readonly MvcOptions _mvcOptions;
    private readonly ILoggerFactory _loggerFactory;

    public ResponseCacheFilterApplicationModelProvider(IOptions<MvcOptions> mvcOptionsAccessor, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(mvcOptionsAccessor);

        _mvcOptions = mvcOptionsAccessor.Value;
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    // The order is set to execute after the DefaultPageApplicationModelProvider.
    public int Order => -1000 + 10;

    public void OnProvidersExecuting(PageApplicationModelProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var pageModel = context.PageApplicationModel;
        var responseCacheAttributes = pageModel.HandlerTypeAttributes.OfType<ResponseCacheAttribute>();
        foreach (var attribute in responseCacheAttributes)
        {
            var cacheProfile = attribute.GetCacheProfile(_mvcOptions);
            context.PageApplicationModel.Filters.Add(new PageResponseCacheFilter(cacheProfile, _loggerFactory));
        }
    }

    public void OnProvidersExecuted(PageApplicationModelProviderContext context)
    {
    }
}
