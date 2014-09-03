// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// Default implementation of <see cref="IViewLocationCache"/>.
    /// </summary>
    public class DefaultViewLocationCache : IViewLocationCache
    {
        private const char CacheKeySeparator = ':';

        // A mapping of keys generated from ViewLocationExpanderContext to view locations.
        private readonly ConcurrentDictionary<string, string> _cache;

        /// <summary>
        /// Initializes a new instance of <see cref="DefaultViewLocationCache"/>.
        /// </summary>
        public DefaultViewLocationCache()
        {
            _cache = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
        }

        /// <inheritdoc />
        public string Get([NotNull] ViewLocationExpanderContext context)
        {
            var cacheKey = GenerateKey(context);
            string result;
            _cache.TryGetValue(cacheKey, out result);
            return result;
        }

        /// <inheritdoc />
        public void Set([NotNull] ViewLocationExpanderContext context,
                        [NotNull] string value)
        {
            var cacheKey = GenerateKey(context);
            _cache.TryAdd(cacheKey, value);
        }

        internal static string GenerateKey(ViewLocationExpanderContext context)
        {
            var keyBuilder = new StringBuilder();
            var routeValues = context.ActionContext.RouteData.Values;
            var controller = routeValues.GetValueOrDefault<string>(RazorViewEngine.ControllerKey);

            // format is "{viewName}:{controllerName}:{areaName}:"
            keyBuilder.Append(context.ViewName)
                      .Append(CacheKeySeparator)
                      .Append(controller);

            var area = routeValues.GetValueOrDefault<string>(RazorViewEngine.AreaKey);
            if (!string.IsNullOrEmpty(area))
            {
                keyBuilder.Append(CacheKeySeparator)
                          .Append(area);
            }

            if (context.Values != null)
            {
                var valuesDictionary = context.Values;
                foreach (var item in valuesDictionary.OrderBy(k => k.Key, StringComparer.Ordinal))
                {
                    keyBuilder.Append(CacheKeySeparator)
                              .Append(item.Key)
                              .Append(CacheKeySeparator)
                              .Append(item.Value);
                }
            }

            var cacheKey = keyBuilder.ToString();
            return cacheKey;
        }
    }
}