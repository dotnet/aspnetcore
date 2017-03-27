// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.TagHelpers.Cache;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.TagHelpers
{
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

        /// <summary>
        /// Creates a new <see cref="CacheTagHelper"/>.
        /// </summary>
        /// <param name="memoryCache">The <see cref="IMemoryCache"/>.</param>
        /// <param name="htmlEncoder">The <see cref="HtmlEncoder"/> to use.</param>
        public CacheTagHelper(IMemoryCache memoryCache, HtmlEncoder htmlEncoder) : base(htmlEncoder)
        {
            MemoryCache = memoryCache;
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
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            IHtmlContent content = null;

            if (Enabled)
            {
                var cacheKey = new CacheTagKey(this, context);

                MemoryCacheEntryOptions options;

                while (content == null)
                {
                    Task<IHtmlContent> result = null;

                    if (!MemoryCache.TryGetValue(cacheKey, out result))
                    {
                        var tokenSource = new CancellationTokenSource();

                        // Create an entry link scope and flow it so that any tokens related to the cache entries
                        // created within this scope get copied to this scope.

                        options = GetMemoryCacheEntryOptions();
                        options.AddExpirationToken(new CancellationChangeToken(tokenSource.Token));

                        var tcs = new TaskCompletionSource<IHtmlContent>();

                        // The returned value is ignored, we only do this so that
                        // the compiler doesn't complain about the returned task
                        // not being awaited
                        var localTcs = MemoryCache.Set(cacheKey, tcs.Task, options);

                        try
                        {
                            // The entry is set instead of assigning a value to the
                            // task so that the expiration options are are not impacted
                            // by the time it took to compute it.

                            using (var entry = MemoryCache.CreateEntry(cacheKey))
                            {
                                // The result is processed inside an entry
                                // such that the tokens are inherited.

                                result = ProcessContentAsync(output);

                                entry.SetOptions(options);
                                entry.Value = result;

                                content = await result;
                            }
                        }
                        catch
                        {
                            // Remove the worker task from the cache in case it can't complete.
                            tokenSource.Cancel();
                            throw;
                        }
                        finally
                        {
                            // If an exception occurs, ensure the other awaiters
                            // render the output by themselves.
                            tcs.SetResult(null);
                        }
                    }
                    else
                    {
                        // There is either some value already cached (as a Task)
                        // or a worker processing the output. In the case of a worker,
                        // the result will be null, and the request will try to acquire
                        // the result from memory another time.

                        content = await result;
                    }
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

        // Internal for unit testing
        internal MemoryCacheEntryOptions GetMemoryCacheEntryOptions()
        {
            var options = new MemoryCacheEntryOptions();
            if (ExpiresOn != null)
            {
                options.SetAbsoluteExpiration(ExpiresOn.Value);
            }

            if (ExpiresAfter != null)
            {
                options.SetAbsoluteExpiration(ExpiresAfter.Value);
            }

            if (ExpiresSliding != null)
            {
                options.SetSlidingExpiration(ExpiresSliding.Value);
            }

            if (Priority != null)
            {
                options.SetPriority(Priority.Value);
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

        private class CharBufferTextWriter : TextWriter
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

        private class CharBufferHtmlContent : IHtmlContent
        {
            private readonly PagedCharBuffer _buffer;

            public CharBufferHtmlContent(PagedCharBuffer buffer)
            {
                _buffer = buffer;
            }

            public void WriteTo(TextWriter writer, HtmlEncoder encoder)
            {
                var length = _buffer.Length;
                if (length == 0)
                {
                    return;
                }

                for (var i = 0; i < _buffer.Pages.Count; i++)
                {
                    var page = _buffer.Pages[i];
                    var pageLength = Math.Min(length, page.Length);
                    writer.Write(page, index: 0, count: pageLength);
                    length -= pageLength;
                }

                Debug.Assert(length == 0);
            }
        }
    }
}