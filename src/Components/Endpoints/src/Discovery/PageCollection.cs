// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;

namespace Microsoft.AspNetCore.Components;

internal class PageCollection
{
    public PageCollection(IEnumerable<PageDefinition> pages)
    {
        Pages = pages.ToList();
    }

    internal static PageCollection Empty { get; } = new PageCollection(Enumerable.Empty<PageDefinition>());

    internal List<PageDefinition> Pages { get; }
}
