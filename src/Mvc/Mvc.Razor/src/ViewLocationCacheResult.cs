// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Razor;

/// <summary>
/// Result of view location cache lookup.
/// </summary>
internal sealed class ViewLocationCacheResult
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
        ArgumentNullException.ThrowIfNull(viewStarts);

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
        ArgumentNullException.ThrowIfNull(searchedLocations);

        SearchedLocations = searchedLocations;
    }

    /// <summary>
    /// <see cref="ViewLocationCacheItem"/> for the located view.
    /// </summary>
    /// <remarks>Uninitialized when <see cref="Success"/> is <c>false</c>.</remarks>
    public ViewLocationCacheItem ViewEntry { get; }

    /// <summary>
    /// <see cref="ViewLocationCacheItem"/>s for applicable _ViewStarts.
    /// </summary>
    /// <remarks><c>null</c> if <see cref="Success"/> is <c>false</c>.</remarks>
    public IReadOnlyList<ViewLocationCacheItem>? ViewStartEntries { get; }

    /// <summary>
    /// The sequence of locations that were searched.
    /// </summary>
    /// <remarks>
    /// When <see cref="Success"/> is <c>true</c> this includes all paths that were search prior to finding
    /// a view at <see cref="ViewEntry"/>. When <see cref="Success"/> is <c>false</c>, this includes
    /// all search paths.
    /// </remarks>
    public IEnumerable<string>? SearchedLocations { get; }

    /// <summary>
    /// Gets a value that indicates whether the view was successfully found.
    /// </summary>
    public bool Success { get; }
}
