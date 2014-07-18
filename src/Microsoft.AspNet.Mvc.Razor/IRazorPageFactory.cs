// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// Defines methods that are used for creating <see cref="RazorPage"/> instances at a given path.
    /// </summary>
    public interface IRazorPageFactory
    {
        /// <summary>
        /// Creates a <see cref="RazorPage"/> for the specified path.
        /// </summary>
        /// <param name="viewPath">The path to locate the RazorPage.</param>
        /// <returns>The RazorPage instance if it exists, null otherwise.</returns>
        RazorPage CreateInstance(string viewPath);
    }
}
