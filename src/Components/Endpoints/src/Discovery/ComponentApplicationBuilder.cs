// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Builder used to configure a <see cref="RazorComponentApplication"/> instance.
/// </summary>
public class ComponentApplicationBuilder
{
    private readonly HashSet<string> _assemblies = new();
    private PageCollection? _pages;

    // TODO: When we support proper discovery this will be public
    // (and probably have a different shape).
    internal void AddAssembly(string name)
    {
        _assemblies.Add(name);
    }

    /// <summary>
    /// Builds the component application definition.
    /// </summary>
    /// <returns>The <see cref="RazorComponentApplication"/>.</returns>
    public RazorComponentApplication Build()
    {
        return new RazorComponentApplication(_pages ?? PageCollection.Empty);
    }

    internal void RegisterPages(PageCollection pages)
    {
        _pages = pages;
    }
}
