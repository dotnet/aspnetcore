// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// The definition of a component based application.
/// </summary>
public class RazorComponentApplication
{
    private readonly PageCollection _pageCollection;

    // TODO: we define the concepts explicitly (like the collection of pages)
    // In the future we need to decide if we want to do generalize this concept and use
    // something "generic" like a list of "features".
    internal RazorComponentApplication(PageCollection pagesCollection)
    {
        _pageCollection = pagesCollection;
    }

    /// <summary>
    /// Gets the list of <see cref="PageDefinition"/> associated with the application.
    /// </summary>
    /// <returns>The list of pages.</returns>
    public IEnumerable<PageDefinition> Pages => _pageCollection.Pages;
}
