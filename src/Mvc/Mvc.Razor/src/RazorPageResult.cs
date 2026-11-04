// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Razor;

/// <summary>
/// Result of locating a <see cref="IRazorPage"/>.
/// </summary>
public readonly struct RazorPageResult
{
    /// <summary>
    /// Initializes a new instance of <see cref="RazorPageResult"/> for a successful discovery.
    /// </summary>
    /// <param name="name">The name of the page that was found.</param>
    /// <param name="page">The located <see cref="IRazorPage"/>.</param>
    public RazorPageResult(string name, IRazorPage page)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(page);

        Name = name;
        Page = page;
        SearchedLocations = null;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="RazorPageResult"/> for an unsuccessful discovery.
    /// </summary>
    /// <param name="name">The name of the page that was not found.</param>
    /// <param name="searchedLocations">The locations that were searched.</param>
    public RazorPageResult(string name, IEnumerable<string> searchedLocations)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(searchedLocations);

        Name = name;
        Page = null;
        SearchedLocations = searchedLocations;
    }

    /// <summary>
    /// Gets the name or the path of the page being located.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the <see cref="IRazorPage"/> if found.
    /// </summary>
    /// <remarks>This property is <c>null</c> if the page was not found.</remarks>
    public IRazorPage? Page { get; }

    /// <summary>
    /// Gets the locations that were searched when <see cref="Page"/> could not be found.
    /// </summary>
    /// <remarks>This property is <c>null</c> if the page was found.</remarks>
    public IEnumerable<string>? SearchedLocations { get; }
}
