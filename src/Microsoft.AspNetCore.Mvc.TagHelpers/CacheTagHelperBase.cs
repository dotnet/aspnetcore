// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.TagHelpers
{
    /// <summary>
    /// <see cref="TagHelper"/> base implementation for caching elements.
    /// </summary>
    public abstract class CacheTagHelperBase : TagHelper
    {
        private const string VaryByAttributeName = "vary-by";
        private const string VaryByHeaderAttributeName = "vary-by-header";
        private const string VaryByQueryAttributeName = "vary-by-query";
        private const string VaryByRouteAttributeName = "vary-by-route";
        private const string VaryByCookieAttributeName = "vary-by-cookie";
        private const string VaryByUserAttributeName = "vary-by-user";
        private const string ExpiresOnAttributeName = "expires-on";
        private const string ExpiresAfterAttributeName = "expires-after";
        private const string ExpiresSlidingAttributeName = "expires-sliding";
        private const string CacheKeyTokenSeparator = "||";
        private const string EnabledAttributeName = "enabled";
        private static readonly char[] AttributeSeparator = new[] { ',' };

        /// <summary>
        /// Creates a new <see cref="CacheTagHelperBase"/>.
        /// </summary>
        /// <param name="htmlEncoder">The <see cref="HtmlEncoder"/> to use.</param>
        public CacheTagHelperBase(HtmlEncoder htmlEncoder)
        {
            HtmlEncoder = htmlEncoder;
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
        /// Gets the <see cref="System.Text.Encodings.Web.HtmlEncoder"/> which encodes the content to be cached.
        /// </summary>
        protected HtmlEncoder HtmlEncoder { get; }

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
        /// <see cref="Http.HttpContext.User"/>.
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
        /// Gets or sets the value which determines if the tag helper is enabled or not.
        /// </summary>
        [HtmlAttributeName(EnabledAttributeName)]
        public bool Enabled { get; set; } = true;

        // Internal for unit testing
        protected internal string GenerateKey(TagHelperContext context)
        {
            var builder = new StringBuilder(GetKeyPrefix(context));
            builder
                .Append(CacheKeyTokenSeparator)
                .Append(GetUniqueId(context));

            var request = ViewContext.HttpContext.Request;

            if (!string.IsNullOrEmpty(VaryBy))
            {
                builder
                    .Append(CacheKeyTokenSeparator)
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
                builder
                    .Append(CacheKeyTokenSeparator)
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

        protected abstract string GetUniqueId(TagHelperContext context);

        protected abstract string GetKeyPrefix(TagHelperContext context);

        protected static void AddStringCollectionKey(
            StringBuilder builder,
            string keyName,
            string value,
            IDictionary<string, StringValues> sourceCollection)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }
            
            // keyName(param1=value1|param2=value2)
            builder
                .Append(CacheKeyTokenSeparator)
                .Append(keyName)
                .Append("(");

            var values = Tokenize(value);

            // Perf: Avoid allocating enumerator
            for (var i = 0; i < values.Count; i++)
            {
                var item = values[i];
                builder
                    .Append(item)
                    .Append(CacheKeyTokenSeparator)
                    .Append(sourceCollection[item])
                    .Append(CacheKeyTokenSeparator);
            }

            if (values.Count > 0)
            {
                // Remove the trailing separator
                builder.Length -= CacheKeyTokenSeparator.Length;
            }

            builder.Append(")");
        }

        protected static void AddStringCollectionKey<TSourceCollection>(
            StringBuilder builder,
            string keyName,
            string value,
            TSourceCollection sourceCollection,
            Func<TSourceCollection, string, StringValues> accessor)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            // keyName(param1=value1|param2=value2)
            builder
                .Append(CacheKeyTokenSeparator)
                .Append(keyName)
                .Append("(");

            var values = Tokenize(value);

            // Perf: Avoid allocating enumerator
            for (var i = 0; i < values.Count; i++)
            {
                var item = values[i];

                builder
                    .Append(item)
                    .Append(CacheKeyTokenSeparator)
                    .Append(accessor(sourceCollection, item))
                    .Append(CacheKeyTokenSeparator);
            }

            if (values.Count > 0)
            {
                // Remove the trailing separator
                builder.Length -= CacheKeyTokenSeparator.Length;
            }

            builder.Append(")");
        }

        protected static IList<string> Tokenize(string value)
        {
            var values = value.Split(AttributeSeparator, StringSplitOptions.RemoveEmptyEntries);
            if (values.Length == 0)
            {
                return values;
            }

            var trimmedValues = new List<string>();

            for (var i = 0; i < values.Length; i++)
            {
                var trimmedValue = values[i].Trim();

                if (trimmedValue.Length > 0)
                {
                    trimmedValues.Add(trimmedValue);
                }
            }

            return trimmedValues;
        }
        
        protected void AddVaryByRouteKey(StringBuilder builder)
        {
            var tokenFound = false;

            if (string.IsNullOrEmpty(VaryByRoute))
            {
                return;
            }
            
            builder
                .Append(CacheKeyTokenSeparator)
                .Append(nameof(VaryByRoute))
                .Append("(");

            var varyByRoutes = Tokenize(VaryByRoute);
            for (var i = 0; i < varyByRoutes.Count; i++)
            {
                var route = varyByRoutes[i];
                tokenFound = true;

                builder
                    .Append(route)
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
}