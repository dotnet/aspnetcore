// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.TagHelpers.Cache
{
    /// <summary>
    /// Implements <see cref="IDistributedCacheTagHelperService"/> and ensures
    /// multiple concurrent requests are gated.
    /// The entries are stored like this:
    /// <list type="bullet">
    /// <item>
    /// <description>Int32 representing the hashed cache key size.</description>
    /// </item>
    /// <item>
    /// <description>The UTF8 encoded hashed cache key.</description>
    /// </item>
    /// <item>
    /// <description>The UTF8 encoded cached content.</description>
    /// </item>
    /// </list>
    /// </summary>
    public class DistributedCacheTagHelperService : IDistributedCacheTagHelperService
    {
        private readonly IDistributedCacheTagHelperStorage _storage;
        private readonly IDistributedCacheTagHelperFormatter _formatter;
        private readonly HtmlEncoder _htmlEncoder;
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<CacheTagKey, Task<IHtmlContent>> _workers;

        public DistributedCacheTagHelperService(
            IDistributedCacheTagHelperStorage storage,
            IDistributedCacheTagHelperFormatter formatter,
            HtmlEncoder HtmlEncoder,
            ILoggerFactory loggerFactory)
        {
            if (storage == null)
            {
                throw new ArgumentNullException(nameof(storage));
            }

            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            if (HtmlEncoder == null)
            {
                throw new ArgumentNullException(nameof(HtmlEncoder));
            }

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _formatter = formatter;
            _storage = storage;
            _htmlEncoder = HtmlEncoder;
            _logger = loggerFactory.CreateLogger<DistributedCacheTagHelperService>();
            _workers = new ConcurrentDictionary<CacheTagKey, Task<IHtmlContent>>();
        }

        /// <inheritdoc />
        public async Task<IHtmlContent> ProcessContentAsync(TagHelperOutput output, CacheTagKey key, DistributedCacheEntryOptions options)
        {
            IHtmlContent content = null;

            while (content == null)
            {
                // Is there any request already processing the value?
                if (!_workers.TryGetValue(key, out var result))
                {
                    // There is a small race condition here between TryGetValue and TryAdd that might cause the
                    // content to be computed more than once. We don't care about this race as the probability of
                    // happening is very small and the impact is not critical.
                    var tcs = new TaskCompletionSource<IHtmlContent>(creationOptions: TaskCreationOptions.RunContinuationsAsynchronously);

                    _workers.TryAdd(key, tcs.Task);

                    try
                    {
                        var serializedKey = Encoding.UTF8.GetBytes(key.GenerateKey());
                        var storageKey = key.GenerateHashedKey();
                        var value = await _storage.GetAsync(storageKey);

                        if (value == null)
                        {
                            // The value is not cached, we need to render the tag helper output
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

                            // Then cache the result
                            value = await _formatter.SerializeAsync(formattingContext);

                            var encodeValue = Encode(value, serializedKey);

                            await _storage.SetAsync(storageKey, encodeValue, options);

                            content = formattingContext.Html;
                        }
                        else
                        {
                            // The value was found in the storage, decode and ensure
                            // there is no cache key hash collision
                            byte[] decodedValue = Decode(value, serializedKey);

                            try
                            {
                                if (decodedValue != null)
                                {
                                    content = await _formatter.DeserializeAsync(decodedValue);
                                }
                            }
                            catch (Exception e)
                            {
                                _logger.DistributedFormatterDeserializationException(storageKey, e);
                            }
                            finally
                            {
                                // If the deserialization fails the content is rendered
                                if (content == null)
                                {
                                    content = await output.GetChildContentAsync();
                                }
                            }
                        }
                    }
                    catch
                    {
                        content = null;
                        throw;
                    }
                    finally
                    {
                        // Remove the worker task before setting the result.
                        // If the result is null, other threads would potentially
                        // acquire it otherwise.
                        _workers.TryRemove(key, out result);

                        // Notify all other awaiters to render the content
                        tcs.TrySetResult(content);
                    }
                }
                else
                {
                    content = await result;
                }
            }

            return content;
        }

        private byte[] Encode(byte[] value, byte[] serializedKey)
        {
            using (var buffer = new MemoryStream())
            {
                var keyLength = BitConverter.GetBytes(serializedKey.Length);

                buffer.Write(keyLength, 0, keyLength.Length);
                buffer.Write(serializedKey, 0, serializedKey.Length);
                buffer.Write(value, 0, value.Length);

                return buffer.ToArray();
            }
        }

        private byte[] Decode(byte[] value, byte[] expectedKey)
        {
            byte[] decoded = null;

            using (var buffer = new MemoryStream(value))
            {
                var keyLengthBuffer = new byte[sizeof(int)];
                buffer.Read(keyLengthBuffer, 0, keyLengthBuffer.Length);

                var keyLength = BitConverter.ToInt32(keyLengthBuffer, 0);
                var serializedKeyBuffer = new byte[keyLength];
                buffer.Read(serializedKeyBuffer, 0, serializedKeyBuffer.Length);

                // Ensure we are reading the expected key before continuing
                if (serializedKeyBuffer.SequenceEqual(expectedKey))
                {
                    decoded = new byte[value.Length - keyLengthBuffer.Length - serializedKeyBuffer.Length];
                    buffer.Read(decoded, 0, decoded.Length);
                }
            }

            return decoded;
        }
    }
}
