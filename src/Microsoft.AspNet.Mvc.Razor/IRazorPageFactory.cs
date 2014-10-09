// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// Defines methods that are used for creating <see cref="IRazorPage"/> instances at a given path.
    /// </summary>
    public interface IRazorPageFactory
    {
        /// <summary>
        /// Creates a <see cref="IRazorPage"/> for the specified path.
        /// </summary>
        /// <param name="relativePath">The path to locate the page.</param>
        /// <returns>The IRazorPage instance if it exists, null otherwise.</returns>
        IRazorPage CreateInstance(string relativePath);
    }
}
