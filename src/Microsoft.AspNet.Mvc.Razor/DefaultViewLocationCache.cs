// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// Default implementation of <see cref="IViewLocationCache"/>.
    /// </summary>
    public class DefaultViewLocationCache : IViewLocationCache
    {
        // A mapping of keys generated from ViewLocationExpanderContext to view locations.
        private readonly ConcurrentDictionary<ViewLocationCacheKey, ViewLocationCacheResult> _cache;

        /// <summary>
        /// Initializes a new instance of <see cref="DefaultViewLocationCache"/>.
        /// </summary>
        public DefaultViewLocationCache()
        {
            _cache = new ConcurrentDictionary<ViewLocationCacheKey, ViewLocationCacheResult>(
                ViewLocationCacheKeyComparer.Instance);
        }

        /// <inheritdoc />
        public ViewLocationCacheResult Get(ViewLocationExpanderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var cacheKey = GenerateKey(context, copyViewExpanderValues: false);
            ViewLocationCacheResult result;
            if (_cache.TryGetValue(cacheKey, out result))
            {
                return result;
            }

            return ViewLocationCacheResult.None;
        }

        /// <inheritdoc />
        public void Set(
            ViewLocationExpanderContext context,
            ViewLocationCacheResult value)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var cacheKey = GenerateKey(context, copyViewExpanderValues: true);
            _cache.TryAdd(cacheKey, value);
        }

        // Internal for unit testing
        internal static ViewLocationCacheKey GenerateKey(
            ViewLocationExpanderContext context,
            bool copyViewExpanderValues)
        {
            var controller = RazorViewEngine.GetNormalizedRouteValue(
                context.ActionContext,
                RazorViewEngine.ControllerKey);

            var area = RazorViewEngine.GetNormalizedRouteValue(
                context.ActionContext,
                RazorViewEngine.AreaKey);


            var values = context.Values;
            if (values != null && copyViewExpanderValues)
            {
                // When performing a Get, avoid creating a copy of the values dictionary
                values = new Dictionary<string, string>(values, StringComparer.Ordinal);
            }

            return new ViewLocationCacheKey(
                context.ViewName,
                controller,
                area,
                context.IsPartial,
                values);
        }

        // Internal for unit testing
        internal class ViewLocationCacheKeyComparer : IEqualityComparer<ViewLocationCacheKey>
        {
            public static readonly ViewLocationCacheKeyComparer Instance = new ViewLocationCacheKeyComparer();

            public bool Equals(ViewLocationCacheKey x, ViewLocationCacheKey y)
            {
                if (x.IsPartial != y.IsPartial ||
                    !string.Equals(x.ViewName, y.ViewName, StringComparison.Ordinal) ||
                    !string.Equals(x.ControllerName, y.ControllerName, StringComparison.Ordinal) ||
                    !string.Equals(x.AreaName, y.AreaName, StringComparison.Ordinal))
                {
                    return false;
                }

                if (ReferenceEquals(x.Values, y.Values))
                {
                    return true;
                }

                if (x.Values == null || y.Values == null || (x.Values.Count != y.Values.Count))
                {
                    return false;
                }

                foreach (var item in x.Values)
                {
                    string yValue;
                    if (!y.Values.TryGetValue(item.Key, out yValue) ||
                        !string.Equals(item.Value, yValue, StringComparison.Ordinal))
                    {
                        return false;
                    }
                }

                return true;
            }

            public int GetHashCode(ViewLocationCacheKey key)
            {
                var hashCodeCombiner = HashCodeCombiner.Start();
                hashCodeCombiner.Add(key.IsPartial ? 1 : 0);
                hashCodeCombiner.Add(key.ViewName, StringComparer.Ordinal);
                hashCodeCombiner.Add(key.ControllerName, StringComparer.Ordinal);
                hashCodeCombiner.Add(key.AreaName, StringComparer.Ordinal);

                if (key.Values != null)
                {
                    foreach (var item in key.Values)
                    {
                        hashCodeCombiner.Add(item.Key, StringComparer.Ordinal);
                        hashCodeCombiner.Add(item.Value, StringComparer.Ordinal);
                    }
                }

                return hashCodeCombiner;
            }
        }

        // Internal for unit testing
        internal struct ViewLocationCacheKey
        {
            public ViewLocationCacheKey(
                string viewName,
                string controllerName,
                string areaName,
                bool isPartial,
                IDictionary<string, string> values)
            {
                ViewName = viewName;
                ControllerName = controllerName;
                AreaName = areaName;
                IsPartial = isPartial;
                Values = values;
            }

            public string ViewName { get; }

            public string ControllerName { get; }

            public string AreaName { get; }

            public bool IsPartial { get; }

            public IDictionary<string, string> Values { get; }
        }
    }
}