// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// Result of <see cref="IViewLocationCache"/> lookups.
    /// </summary>
    public struct ViewLocationCacheResult : IEquatable<ViewLocationCacheResult>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ViewLocationCacheResult"/>
        /// for a view that was successfully found at the specified location.
        /// </summary>
        /// <param name="foundLocation">The view location.</param>
        /// <param name="searchedLocations">Locations that were searched
        /// in addition to <paramref name="foundLocation"/>.</param>
        public ViewLocationCacheResult(
            [NotNull] string foundLocation,
            [NotNull] IEnumerable<string> searchedLocations)
            : this (searchedLocations)
        {
            ViewLocation = foundLocation;
            SearchedLocations = searchedLocations;
            IsFoundResult = true;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ViewLocationCacheResult"/> for a
        /// failed view lookup.
        /// </summary>
        /// <param name="searchedLocations">Locations that were searched.</param>
        public ViewLocationCacheResult([NotNull] IEnumerable<string> searchedLocations)
        {
            SearchedLocations = searchedLocations;
            ViewLocation = null;
            IsFoundResult = false;
        }

        /// <summary>
        /// A <see cref="ViewLocationCacheResult"/> that represents a cache miss.
        /// </summary>
        public static readonly ViewLocationCacheResult None = new ViewLocationCacheResult(Enumerable.Empty<string>());

        /// <summary>
        /// The location the view was found.
        /// </summary>
        /// <remarks>This is available if <see cref="IsFoundResult"/> is <c>true</c>.</remarks>
        public string ViewLocation { get; }

        /// <summary>
        /// The sequence of locations that were searched.
        /// </summary>
        /// <remarks>
        /// When <see cref="IsFoundResult"/> is <c>true</c> this includes all paths that were search prior to finding
        /// a view at <see cref="ViewLocation"/>. When <see cref="IsFoundResult"/> is <c>false</c>, this includes
        /// all search paths.
        /// </remarks>
        public IEnumerable<string> SearchedLocations { get; }

        /// <summary>
        /// Gets a value that indicates whether the view was successfully found.
        /// </summary>
        public bool IsFoundResult { get; }

        /// <inheritdoc />
        public bool Equals(ViewLocationCacheResult other)
        {
            if (IsFoundResult != other.IsFoundResult)
            {
                return false;
            }

            if (IsFoundResult)
            {
                return string.Equals(ViewLocation, other.ViewLocation, StringComparison.Ordinal);
            }
            else
            {
                if (SearchedLocations == other.SearchedLocations)
                {
                    return true;
                }

                if (SearchedLocations == null || other.SearchedLocations == null)
                {
                    return false;
                }

                return Enumerable.SequenceEqual(SearchedLocations, other.SearchedLocations, StringComparer.Ordinal);
            }
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hashCodeCombiner = HashCodeCombiner.Start()
                .Add(IsFoundResult);


            if (IsFoundResult)
            {
                hashCodeCombiner.Add(ViewLocation, StringComparer.Ordinal);
            }
            else if (SearchedLocations != null)
            {
                foreach (var location in SearchedLocations)
                {
                    hashCodeCombiner.Add(location, StringComparer.Ordinal);
                }
            }

            return hashCodeCombiner;
        }
    }
}
