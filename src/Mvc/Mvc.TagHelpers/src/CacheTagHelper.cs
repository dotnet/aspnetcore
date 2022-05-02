// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.TagHelpers.Cache;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.TagHelpers;

/// <summary>
/// <see cref="TagHelper"/> implementation targeting &lt;cache&gt; elements.
/// </summary>
public class CacheTagHelper : CacheTagHelperBase
{
    /// <summary>
    /// Prefix used by <see cref="CacheTagHelper"/> instances when creating entries in <see cref="MemoryCache"/>.
    /// </summary>
    public static readonly string CacheKeyPrefix = nameof(CacheTagHelper);

    private const string CachePriorityAttributeName = "priority";

    // We need to come up with a value for the size of entries when storing a gating Task on the cache. Any value
    // greater than 0 will suffice. We choose 56 bytes as an approximation of the size of the task that we store
    // in the cache. This size got calculated as an upper bound for the size of an actual task on an x64 architecture
    // and corresponds to 24 bytes for the object header block plus the 40 bytes added by the members of the task
    // object.
    private const int PlaceholderSize = 64;

    /// <summary>
    /// Creates a new <see cref="CacheTagHelper"/>.
    /// </summary>
    /// <param name="factory">The factory containing the private <see cref="IMemoryCache"/> instance
    /// used by the <see cref="CacheTagHelper"/>.</param>
    /// <param name="htmlEncoder">The <see cref="HtmlEncoder"/> to use.</param>
    public CacheTagHelper(
        CacheTagHelperMemoryCacheFactory factory,
        HtmlEncoder htmlEncoder)
        : base(htmlEncoder)
    {
        MemoryCache = factory.Cache;
    }

    /// <summary>
    /// Gets the <see cref="IMemoryCache"/> instance used to cache entries.
    /// </summary>
    protected IMemoryCache MemoryCache { get; }

    /// <summary>
    /// Gets or sets the <see cref="CacheItemPriority"/> policy for the cache entry.
    /// </summary>
    [HtmlAttributeName(CachePriorityAttributeName)]
    public CacheItemPriority? Priority { get; set; }

    /// <inheritdoc />
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(output);

        IHtmlContent content;
        if (Enabled)
        {
            var cacheKey = new CacheTagKey(this, context);
            if (MemoryCache.TryGetValue(cacheKey, out Task<IHtmlContent> cachedResult))
            {
                // There is either some value already cached (as a Task) or a worker processing the output.
                content = await cachedResult;
            }
            else
            {
                content = await CreateCacheEntry(cacheKey, output);
            }
        }
        else
        {
            content = await output.GetChildContentAsync();
        }

        // Clear the contents of the "cache" element since we don't want to render it.
        output.SuppressOutput();
        output.Content.SetHtmlContent(content);
    }

    private async Task<IHtmlContent> CreateCacheEntry(CacheTagKey cacheKey, TagHelperOutput output)
    {
        var tokenSource = new CancellationTokenSource();

        var options = GetMemoryCacheEntryOptions();
        options.AddExpirationToken(new CancellationChangeToken(tokenSource.Token));
        options.SetSize(PlaceholderSize);
        var tcs = new TaskCompletionSource<IHtmlContent>(creationOptions: TaskCreationOptions.RunContinuationsAsynchronously);

        // The returned value is ignored, we only do this so that
        // the compiler doesn't complain about the returned task
        // not being awaited
        _ = MemoryCache.Set(cacheKey, tcs.Task, options);

        IHtmlContent content;
        try
        {
            // The entry is set instead of assigning a value to the
            // task so that the expiration options are not impacted
            // by the time it took to compute it.

            // Use the CreateEntry to ensure a cache scope is created that will copy expiration tokens from
            // cache entries created from the GetChildContentAsync call to the current entry.
            var entry = MemoryCache.CreateEntry(cacheKey);

            // The result is processed inside an entry
            // such that the tokens are inherited.

            var result = ProcessContentAsync(output);
            content = await result;
            options.SetSize(GetSize(content));
            entry.SetOptions(options);

            entry.Value = result;

            // An entry gets committed to the cache when disposed gets called. We only want to do this when
            // the content has been correctly generated (didn't throw an exception). For that reason the entry
            // can't be put inside a using block.
            entry.Dispose();

            // Set the result on the TCS once we've committed the entry to the cache since commiting to the cache
            // may throw.
            tcs.SetResult(content);
            return content;
        }
        catch (Exception ex)
        {
            // Remove the worker task from the cache in case it can't complete.
            tokenSource.Cancel();

            // Fail the TCS so other awaiters see the exception.
            tcs.TrySetException(ex);
            throw;
        }
        finally
        {
            // The tokenSource needs to be disposed as the MemoryCache
            // will register a callback on the Token.
            tokenSource.Dispose();
        }
    }

    private long GetSize(IHtmlContent content)
    {
        if (content is CharBufferHtmlContent charBuffer)
        {
            // We need to multiply the size of the buffer
            // by a factor of two due to the fact that
            // characters in .NET are UTF-16 which means
            // every character uses two bytes (surrogates
            // are represented as two characters)
            return charBuffer.Buffer.Length * sizeof(char);
        }

        Debug.Fail($"{nameof(content)} should be an {nameof(CharBufferHtmlContent)}.");
        return -1;
    }

    // Internal for unit testing
    internal MemoryCacheEntryOptions GetMemoryCacheEntryOptions()
    {
        var hasEvictionCriteria = false;
        var options = new MemoryCacheEntryOptions();
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

        if (Priority != null)
        {
            options.SetPriority(Priority.Value);
        }

        if (!hasEvictionCriteria)
        {
            options.SetSlidingExpiration(DefaultExpiration);
        }

        return options;
    }

    private async Task<IHtmlContent> ProcessContentAsync(TagHelperOutput output)
    {
        var content = await output.GetChildContentAsync();

        using (var writer = new CharBufferTextWriter())
        {
            content.WriteTo(writer, HtmlEncoder);
            return new CharBufferHtmlContent(writer.Buffer);
        }
    }

    private sealed class CharBufferTextWriter : TextWriter
    {
        public CharBufferTextWriter()
        {
            Buffer = new PagedCharBuffer(CharArrayBufferSource.Instance);
        }

        public override Encoding Encoding => Null.Encoding;

        public PagedCharBuffer Buffer { get; }

        public override void Write(char value)
        {
            Buffer.Append(value);
        }

        public override void Write(char[] buffer, int index, int count)
        {
            Buffer.Append(buffer, index, count);
        }

        public override void Write(string value)
        {
            Buffer.Append(value);
        }
    }

    private sealed class CharBufferHtmlContent : IHtmlContent
    {
        private readonly PagedCharBuffer _buffer;

        public CharBufferHtmlContent(PagedCharBuffer buffer)
        {
            _buffer = buffer;
        }

        public PagedCharBuffer Buffer => _buffer;

        public void WriteTo(TextWriter writer, HtmlEncoder encoder)
        {
            var length = Buffer.Length;
            if (length == 0)
            {
                return;
            }

            for (var i = 0; i < Buffer.Pages.Count; i++)
            {
                var page = Buffer.Pages[i];
                var pageLength = Math.Min(length, page.Length);
                writer.Write(page, index: 0, count: pageLength);
                length -= pageLength;
            }

            Debug.Assert(length == 0);
        }
    }
}
