// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// Defines methods for locating ViewStart pages that are applicable to a page.
    /// </summary>
    public interface IViewStartProvider
    {
        /// <summary>
        /// Given a view path, returns a sequence of ViewStart instances
        /// that are applicable to the specified view.
        /// </summary>
        /// <param name="path">The path of the page to locate ViewStart files for.</param>
        /// <returns>A sequence of <see cref="IRazorPage"/> that represent ViewStart.</returns>
        IEnumerable<IRazorPage> GetViewStartPages(string path);
    }
}