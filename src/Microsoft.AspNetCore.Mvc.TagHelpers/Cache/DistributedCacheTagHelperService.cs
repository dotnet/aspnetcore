// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Caching.Distributed;

namespace Microsoft.AspNetCore.Mvc.TagHelpers.Cache
{
    /// <summary>
    /// Implements <see cref="IDistributedCacheTagHelperService"/> and ensure
    /// multiple concurrent requests are gated.
    /// </summary>
    public class DistributedCacheTagHelperService : IDistributedCacheTagHelperService
    {
        private readonly IDistributedCacheTagHelperStorage _storage;
        private readonly IDistributedCacheTagHelperFormatter _formatter;
        private readonly HtmlEncoder _htmlEncoder;
        private readonly ConcurrentDictionary<string, Task<IHtmlContent>> _workers;

        public DistributedCacheTagHelperService(
            IDistributedCacheTagHelperStorage storage,
            IDistributedCacheTagHelperFormatter formatter,
            HtmlEncoder HtmlEncoder 
        )
        {
            _formatter = formatter;
            _storage = storage;
            _htmlEncoder = HtmlEncoder;

            _workers = new ConcurrentDictionary<string, Task<IHtmlContent>>();
        }

        /// <inheritdoc />
        public async Task<IHtmlContent> ProcessContentAsync(TagHelperOutput output, string key, DistributedCacheEntryOptions options)
        {
            IHtmlContent content = null;

            while (content == null)
            {
                Task<IHtmlContent> result = null;

                // Is there any request already processing the value?
                if (!_workers.TryGetValue(key, out result))
                {
                    var tcs = new TaskCompletionSource<IHtmlContent>();

                    _workers.TryAdd(key, tcs.Task);

                    try
                    {
                        var value = await _storage.GetAsync(key);

                        if (value == null)
                        {
                            var processedContent = await output.GetChildContentAsync();

                            var stringBuilder = new StringBuilder();
                            using (var writer = new StringWriter(stringBuilder))
                            {
                                processedContent.WriteTo(writer, _htmlEncoder);
                            }

                            var formattingContext = new DistributedCacheTagHelperFormattingContext
                            {
                                Html = new HtmlString(stringBuilder.ToString())
                            };

                            value = await _formatter.SerializeAsync(formattingContext);

                            await _storage.SetAsync(key, value, options);

                            content = formattingContext.Html;
                        }
                        else
                        {
                            content = await _formatter.DeserializeAsync(value);

                            // If the deserialization fails, it can return null, for instance when the 
                            // value is not in the expected format.
                            if (content == null)
                            {
                                content = await output.GetChildContentAsync();
                            }
                        }

                        tcs.TrySetResult(content);
                    }
                    catch
                    {
                        tcs.TrySetResult(null);
                        throw;
                    }
                    finally
                    {
                        // Remove the worker task from the in-memory cache
                        Task<IHtmlContent> worker;
                        _workers.TryRemove(key, out worker);
                    }
                }
                else
                {
                    content = await result;
                }
            }

            return content;
        }
    }
}
