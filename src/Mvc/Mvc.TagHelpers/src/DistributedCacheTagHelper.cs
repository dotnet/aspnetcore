// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.TagHelpers.Cache;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

namespace Microsoft.AspNetCore.Mvc.TagHelpers;

/// <summary>
/// <see cref="TagHelper"/> implementation targeting &lt;distributed-cache&gt; elements.
/// </summary>
[HtmlTargetElement("distributed-cache", Attributes = NameAttributeName)]
public class DistributedCacheTagHelper : CacheTagHelperBase
{
    private readonly IDistributedCacheTagHelperService _distributedCacheService;

    /// <summary>
    /// Prefix used by <see cref="DistributedCacheTagHelper"/> instances when creating entries in <see cref="IDistributedCacheTagHelperStorage"/>.
    /// </summary>
    public static readonly string CacheKeyPrefix = nameof(DistributedCacheTagHelper);

    private const string NameAttributeName = "name";

    /// <summary>
    /// Creates a new <see cref="CacheTagHelper"/>.
    /// </summary>
    /// <param name="distributedCacheService">The <see cref="IDistributedCacheTagHelperService"/>.</param>
    /// <param name="htmlEncoder">The <see cref="HtmlEncoder"/>.</param>
    public DistributedCacheTagHelper(
        IDistributedCacheTagHelperService distributedCacheService,
        HtmlEncoder htmlEncoder)
        : base(htmlEncoder)
    {
        _distributedCacheService = distributedCacheService;
    }

    /// <summary>
    /// Gets the <see cref="IMemoryCache"/> instance used to cache workers.
    /// </summary>
    protected IMemoryCache MemoryCache { get; }

    /// <summary>
    /// Gets or sets a unique name to discriminate cached entries.
    /// </summary>
    [HtmlAttributeName(NameAttributeName)]
    public string Name { get; set; }

    /// <inheritdoc />
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(output);

        IHtmlContent content;
        if (Enabled)
        {
            var cacheKey = new CacheTagKey(this);

            content = await _distributedCacheService.ProcessContentAsync(output, cacheKey, GetDistributedCacheEntryOptions());
        }
        else
        {
            content = await output.GetChildContentAsync();
        }

        // Clear the contents of the "cache" element since we don't want to render it.
        output.SuppressOutput();

        output.Content.SetHtmlContent(content);
    }

    // Internal for unit testing
    internal DistributedCacheEntryOptions GetDistributedCacheEntryOptions()
    {
        var hasEvictionCriteria = false;
        var options = new DistributedCacheEntryOptions();
        if (ExpiresOn != null)
        {
            hasEvictionCriteria = true;
            options.SetAbsoluteExpiration(ExpiresOn.Value);
        }

        if (ExpiresAfter != null)
        {
            hasEvictionCriteria = true;
            options.SetAbsoluteExpiration(ExpiresAfter.Value);
        }

        if (ExpiresSliding != null)
        {
            hasEvictionCriteria = true;
            options.SetSlidingExpiration(ExpiresSliding.Value);
        }

        if (!hasEvictionCriteria)
        {
            options.SetSlidingExpiration(DefaultExpiration);
        }

        return options;
    }
}
