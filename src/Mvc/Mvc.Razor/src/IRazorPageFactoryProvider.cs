// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Razor
{
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
}
