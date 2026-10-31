// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.TagHelpers.Cache;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace HtmlGenerationWebSite;

// This TagHelper enables us to investigate potential flakiness in the test that uses this tracked by https://github.com/aspnet/Mvc/issues/8281
public class TestCacheTagHelper : CacheTagHelper
{
    private readonly ILogger _logger;

    public TestCacheTagHelper(
        CacheTagHelperMemoryCacheFactory factory,
        HtmlEncoder htmlEncoder,
        ILoggerFactory loggerFactory) : base(factory, htmlEncoder)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _logger = loggerFactory.CreateLogger<TestCacheTagHelper>();
    }

    public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var cacheKey = new CacheTagKey(this, context);
        if (MemoryCache.TryGetValue(cacheKey, out var _))
        {
            _logger.LogInformation("Cache entry exists with key: " + cacheKey.GenerateKey());
        }
        else
        {
            _logger.LogInformation("Cache entry does NOT exist with key: " + cacheKey.GenerateKey());
        }

        return base.ProcessAsync(context, output);
    }
}
