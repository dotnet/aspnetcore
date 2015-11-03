// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Mvc.ViewFeatures;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNet.Mvc.TagHelpers
{
    /// <summary>
    /// <see cref="TagHelper"/> implementation targeting &lt;cache&gt; elements.
    /// </summary>
    public class CacheTagHelper : TagHelper
    {
        /// <summary>
        /// Prefix used by <see cref="CacheTagHelper"/> instances when creating entries in <see cref="MemoryCache"/>.
        /// </summary>
        public static readonly string CacheKeyPrefix = nameof(CacheTagHelper);
        private const string VaryByAttributeName = "vary-by";
        private const string VaryByHeaderAttributeName = "vary-by-header";
        private const string VaryByQueryAttributeName = "vary-by-query";
        private const string VaryByRouteAttributeName = "vary-by-route";
        private const string VaryByCookieAttributeName = "vary-by-cookie";
        private const string VaryByUserAttributeName = "vary-by-user";
        private const string ExpiresOnAttributeName = "expires-on";
        private const string ExpiresAfterAttributeName = "expires-after";
        private const string ExpiresSlidingAttributeName = "expires-sliding";
        private const string CachePriorityAttributeName = "priority";
        private const string CacheKeyTokenSeparator = "||";
        private const string EnabledAttributeName = "enabled";
        private static readonly char[] AttributeSeparator = new[] { ',' };

        /// <summary>
        /// Creates a new <see cref="CacheTagHelper"/>.
        /// </summary>
        /// <param name="memoryCache">The <see cref="IMemoryCache"/>.</param>
        public CacheTagHelper(IMemoryCache memoryCache)
        {
            MemoryCache = memoryCache;
        }

        /// <inheritdoc />
        public override int Order
        {
            get
            {
                return -1000;
            }
        }

        /// <summary>
        /// Gets the <see cref="IMemoryCache"/> instance used to cache entries.
        /// </summary>
        protected IMemoryCache MemoryCache { get; }

        /// <summary>
        /// Gets or sets the <see cref="ViewContext"/> for the current executing View.
        /// </summary>
        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        /// <summary>
        /// Gets or sets a <see cref="string" /> to vary the cached result by.
        /// </summary>
        [HtmlAttributeName(VaryByAttributeName)]
        public string VaryBy { get; set; }

        /// <summary>
        /// Gets or sets the name of a HTTP request header to vary the cached result by.
        /// </summary>
        [HtmlAttributeName(VaryByHeaderAttributeName)]
        public string VaryByHeader { get; set; }

        /// <summary>
        /// Gets or sets a comma-delimited set of query parameters to vary the cached result by.
        /// </summary>
        [HtmlAttributeName(VaryByQueryAttributeName)]
        public string VaryByQuery { get; set; }

        /// <summary>
        /// Gets or sets a comma-delimited set of route data parameters to vary the cached result by.
        /// </summary>
        [HtmlAttributeName(VaryByRouteAttributeName)]
        public string VaryByRoute { get; set; }

        /// <summary>
        /// Gets or sets a comma-delimited set of cookie names to vary the cached result by.
        /// </summary>
        [HtmlAttributeName(VaryByCookieAttributeName)]
        public string VaryByCookie { get; set; }

        /// <summary>
        /// Gets or sets a value that determines if the cached result is to be varied by the Identity for the logged in
        /// <see cref="HttpContext.User"/>.
        /// </summary>
        [HtmlAttributeName(VaryByUserAttributeName)]
        public bool VaryByUser { get; set; }

        /// <summary>
        /// Gets or sets the exact <see cref="DateTimeOffset"/> the cache entry should be evicted.
        /// </summary>
        [HtmlAttributeName(ExpiresOnAttributeName)]
        public DateTimeOffset? ExpiresOn { get; set; }

        /// <summary>
        /// Gets or sets the duration, from the time the cache entry was added, when it should be evicted.
        /// </summary>
        [HtmlAttributeName(ExpiresAfterAttributeName)]
        public TimeSpan? ExpiresAfter { get; set; }

        /// <summary>
        /// Gets or sets the duration from last access that the cache entry should be evicted.
        /// </summary>
        [HtmlAttributeName(ExpiresSlidingAttributeName)]
        public TimeSpan? ExpiresSliding { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="CacheItemPriority"/> policy for the cache entry.
        /// </summary>
        [HtmlAttributeName(CachePriorityAttributeName)]
        public CacheItemPriority? Priority { get; set; }

        /// <summary>
        /// Gets or sets the value which determines if the tag helper is enabled or not.
        /// </summary>
        [HtmlAttributeName(EnabledAttributeName)]
        public bool Enabled { get; set; } = true;

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

            TagHelperContent result = null;
            if (Enabled)
            {
                var key = GenerateKey(context);
                if (!MemoryCache.TryGetValue(key, out result))
                {
                    // Create an entry link scope and flow it so that any tokens related to the cache entries
                    // created within this scope get copied to this scope.
                    using (var link = MemoryCache.CreateLinkingScope())
                    {
                        result = await output.GetChildContentAsync();

                        MemoryCache.Set(key, result, GetMemoryCacheEntryOptions(link));
                    }
                }
            }

            // Clear the contents of the "cache" element since we don't want to render it.
            output.SuppressOutput();
            if (Enabled)
            {
                output.Content.SetContent(result);
            }
            else
            {
                result = await output.GetChildContentAsync();
                output.Content.SetContent(result);
            }
        }

        // Internal for unit testing
        internal string GenerateKey(TagHelperContext context)
        {
            var builder = new StringBuilder(CacheKeyPrefix);
            builder.Append(CacheKeyTokenSeparator)
                   .Append(context.UniqueId);

            var request = ViewContext.HttpContext.Request;

            if (!string.IsNullOrEmpty(VaryBy))
            {
                builder.Append(CacheKeyTokenSeparator)
                       .Append(nameof(VaryBy))
                       .Append(CacheKeyTokenSeparator)
                       .Append(VaryBy);
            }

            AddStringCollectionKey(builder, nameof(VaryByCookie), VaryByCookie, request.Cookies, (c, key) => c[key]);
            AddStringCollectionKey(builder, nameof(VaryByHeader), VaryByHeader, request.Headers, (c, key) => c[key]);
            AddStringCollectionKey(builder, nameof(VaryByQuery), VaryByQuery, request.Query, (c, key) => c[key]);
            AddVaryByRouteKey(builder);

            if (VaryByUser)
            {
                builder.Append(CacheKeyTokenSeparator)
                       .Append(nameof(VaryByUser))
                       .Append(CacheKeyTokenSeparator)
                       .Append(ViewContext.HttpContext.User?.Identity?.Name);
            }

            // The key is typically too long to be useful, so we use a cryptographic hash
            // as the actual key (better randomization and key distribution, so small vary
            // values will generate dramatically different keys).
            using (var sha = SHA256.Create())
            {
                var contentBytes = Encoding.UTF8.GetBytes(builder.ToString());
                var hashedBytes = sha.ComputeHash(contentBytes);
                return Convert.ToBase64String(hashedBytes);
            }
        }

        // Internal for unit testing
        internal MemoryCacheEntryOptions GetMemoryCacheEntryOptions(IEntryLink entryLink)
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

            options.AddEntryLink(entryLink);
            return options;
        }

        private static void AddStringCollectionKey(
            StringBuilder builder,
            string keyName,
            string value,
            IDictionary<string, StringValues> sourceCollection)
        {
            if (!string.IsNullOrEmpty(value))
            {
                // keyName(param1=value1|param2=value2)
                builder.Append(CacheKeyTokenSeparator)
                       .Append(keyName)
                       .Append("(");

                var tokenFound = false;
                foreach (var item in Tokenize(value))
                {
                    tokenFound = true;

                    builder.Append(item)
                           .Append(CacheKeyTokenSeparator)
                           .Append(sourceCollection[item])
                           .Append(CacheKeyTokenSeparator);
                }

                if (tokenFound)
                {
                    // Remove the trailing separator
                    builder.Length -= CacheKeyTokenSeparator.Length;
                }

                builder.Append(")");
            }
        }

        private static void AddStringCollectionKey<T>(
            StringBuilder builder,
            string keyName,
            string value,
            T sourceCollection,
            Func<T, string, StringValues> accessor)
        {
            if (!string.IsNullOrEmpty(value))
            {
                // keyName(param1=value1|param2=value2)
                builder.Append(CacheKeyTokenSeparator)
                       .Append(keyName)
                       .Append("(");

                var tokenFound = false;
                foreach (var item in Tokenize(value))
                {
                    tokenFound = true;

                    builder.Append(item)
                           .Append(CacheKeyTokenSeparator)
                           .Append(accessor(sourceCollection, item))
                           .Append(CacheKeyTokenSeparator);
                }

                if (tokenFound)
                {
                    // Remove the trailing separator
                    builder.Length -= CacheKeyTokenSeparator.Length;
                }

                builder.Append(")");
            }
        }

        private void AddVaryByRouteKey(StringBuilder builder)
        {
            var tokenFound = false;

            if (!string.IsNullOrEmpty(VaryByRoute))
            {
                builder.Append(CacheKeyTokenSeparator)
                       .Append(nameof(VaryByRoute))
                       .Append("(");

                foreach (var route in Tokenize(VaryByRoute))
                {
                    tokenFound = true;

                    builder.Append(route)
                           .Append(CacheKeyTokenSeparator)
                           .Append(ViewContext.RouteData.Values[route])
                           .Append(CacheKeyTokenSeparator);
                }

                if (tokenFound)
                {
                    builder.Length -= CacheKeyTokenSeparator.Length;
                }

                builder.Append(")");
            }
        }

        private static IEnumerable<string> Tokenize(string value)
        {
            return value.Split(AttributeSeparator, StringSplitOptions.RemoveEmptyEntries)
                        .Select(token => token.Trim())
                        .Where(token => token.Length > 0);
        }
    }
}