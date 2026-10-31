// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Razor;

/// <summary>
/// Defines methods that are used for creating <see cref="IRazorPage"/> instances at a given path.
/// </summary>
public interface IRazorPageFactoryProvider
{
    /// <summary>
    /// Creates a <see cref="IRazorPage"/> factory for the specified path.
    /// </summary>
    /// <param name="relativePath">The path to locate the page.</param>
    /// <returns>The <see cref="RazorPageFactoryResult"/> instance.</returns>
    RazorPageFactoryResult CreateFactory(string relativePath);
}
