// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    /// <summary>
    /// An abstraction for working with a project containing Razor files.
    /// </summary>
    public abstract class RazorProject
    {
        /// <summary>
        /// Gets a sequence of <see cref="RazorProjectItem"/> under the specific path in the project.
        /// </summary>
        /// <param name="basePath">The base path.</param>
        /// <returns>The sequence of <see cref="RazorProjectItem"/>.</returns>
        public abstract IEnumerable<RazorProjectItem> EnumerateItems(string basePath);
    }
}