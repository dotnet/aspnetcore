// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    /// <summary>
    /// Result of view location cache lookup.
    /// </summary>
    internal class ViewLocationCacheResult
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ViewLocationCacheResult"/>
        /// for a view that was successfully found at the specified location.
        /// </summary>
        /// <param name="view">The <see cref="ViewLocationCacheItem"/> for the found view.</param>
        /// <param name="viewStarts"><see cref="ViewLocationCacheItem"/>s for applicable _ViewStarts.</param>
        public ViewLocationCacheResult(
            ViewLocationCacheItem view,
            IReadOnlyList<ViewLocationCacheItem> viewStarts)
        {
            if (viewStarts == null)
            {
                throw new ArgumentNullException(nameof(viewStarts));
            }

            ViewEntry = view;
            ViewStartEntries = viewStarts;
            Success = true;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ViewLocationCacheResult"/> for a
        /// failed view lookup.
        /// </summary>
        /// <param name="searchedLocations">Locations that were searched.</param>
        public ViewLocationCacheResult(IEnumerable<string> searchedLocations)
        {
            if (searchedLocations == null)
            {
                throw new ArgumentNullException(nameof(searchedLocations));
            }

            SearchedLocations = searchedLocations;
        }

        /// <summary>
        /// <see cref="ViewLocationCacheItem"/> for the located view.
        /// </summary>
        /// <remarks><c>null</c> if <see cref="Success"/> is <c>false</c>.</remarks>
        public ViewLocationCacheItem ViewEntry { get; }

        /// <summary>
        /// <see cref="ViewLocationCacheItem"/>s for applicable _ViewStarts.
        /// </summary>
        /// <remarks><c>null</c> if <see cref="Success"/> is <c>false</c>.</remarks>
        public IReadOnlyList<ViewLocationCacheItem> ViewStartEntries { get; }

        /// <summary>
        /// The sequence of locations that were searched.
        /// </summary>
        /// <remarks>
        /// When <see cref="Success"/> is <c>true</c> this includes all paths that were search prior to finding
        /// a view at <see cref="ViewEntry"/>. When <see cref="Success"/> is <c>false</c>, this includes
        /// all search paths.
        /// </remarks>
        public IEnumerable<string> SearchedLocations { get; }

        /// <summary>
        /// Gets a value that indicates whether the view was successfully found.
        /// </summary>
        public bool Success { get; }
    }
}
