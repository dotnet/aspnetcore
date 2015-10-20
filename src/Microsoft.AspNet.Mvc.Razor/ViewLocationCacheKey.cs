// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// Key for entries in <see cref="RazorViewEngine.ViewLookupCache"/>.
    /// </summary>
    public struct ViewLocationCacheKey : IEquatable<ViewLocationCacheKey>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ViewLocationCacheKey"/>.
        /// </summary>
        /// <param name="viewName">The view name or path.</param>
        /// <param name="isPartial">Determines if the view is a partial.</param>
        public ViewLocationCacheKey(
            string viewName,
            bool isPartial)
            : this(
                  viewName,
                  controllerName: null,
                  areaName: null,
                  isPartial: isPartial,
                  values: null)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ViewLocationCacheKey"/>.
        /// </summary>
        /// <param name="viewName">The view name.</param>
        /// <param name="controllerName">The controller name.</param>
        /// <param name="areaName">The area name.</param>
        /// <param name="isPartial">Determines if the view is a partial.</param>
        /// <param name="values">Values from <see cref="IViewLocationExpander"/> instances.</param>
        public ViewLocationCacheKey(
            string viewName,
            string controllerName,
            string areaName,
            bool isPartial,
            IReadOnlyDictionary<string, string> values)
        {
            ViewName = viewName;
            ControllerName = controllerName;
            AreaName = areaName;
            IsPartial = isPartial;
            ViewLocationExpanderValues = values;
        }

        /// <summary>
        /// Gets the view name.
        /// </summary>
        public string ViewName { get; }

        /// <summary>
        /// Gets the controller name.
        /// </summary>
        public string ControllerName { get; }

        /// <summary>
        /// Gets the area name.
        /// </summary>
        public string AreaName { get; }

        /// <summary>
        /// Determines if the view is a partial.
        /// </summary>
        public bool IsPartial { get; }

        /// <summary>
        /// Gets the values populated by <see cref="IViewLocationExpander"/> instances.
        /// </summary>
        public IReadOnlyDictionary<string, string> ViewLocationExpanderValues { get; }

        /// <inheritdoc />
        public bool Equals(ViewLocationCacheKey y)
        {
            if (IsPartial != y.IsPartial ||
                !string.Equals(ViewName, y.ViewName, StringComparison.Ordinal) ||
                !string.Equals(ControllerName, y.ControllerName, StringComparison.Ordinal) ||
                !string.Equals(AreaName, y.AreaName, StringComparison.Ordinal))
            {
                return false;
            }

            if (ReferenceEquals(ViewLocationExpanderValues, y.ViewLocationExpanderValues))
            {
                return true;
            }

            if (ViewLocationExpanderValues == null ||
                y.ViewLocationExpanderValues == null ||
                (ViewLocationExpanderValues.Count != y.ViewLocationExpanderValues.Count))
            {
                return false;
            }

            foreach (var item in ViewLocationExpanderValues)
            {
                string yValue;
                if (!y.ViewLocationExpanderValues.TryGetValue(item.Key, out yValue) ||
                    !string.Equals(item.Value, yValue, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (obj is ViewLocationCacheKey)
            {
                return Equals((ViewLocationCacheKey)obj);
            }

            return false;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hashCodeCombiner = HashCodeCombiner.Start();
            hashCodeCombiner.Add(IsPartial ? 1 : 0);
            hashCodeCombiner.Add(ViewName, StringComparer.Ordinal);
            hashCodeCombiner.Add(ControllerName, StringComparer.Ordinal);
            hashCodeCombiner.Add(AreaName, StringComparer.Ordinal);

            if (ViewLocationExpanderValues != null)
            {
                foreach (var item in ViewLocationExpanderValues)
                {
                    hashCodeCombiner.Add(item.Key, StringComparer.Ordinal);
                    hashCodeCombiner.Add(item.Value, StringComparer.Ordinal);
                }
            }

            return hashCodeCombiner;
        }
    }
}
