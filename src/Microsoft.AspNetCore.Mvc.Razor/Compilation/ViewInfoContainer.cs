// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Mvc.ApplicationParts
{
    /// <summary>
    /// A container for <see cref="ViewInfo"/> instances.
    /// </summary>
    public class ViewInfoContainer
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ViewInfos"/>.
        /// </summary>
        /// <param name="views">The sequence of <see cref="ViewInfo"/>.</param>
        public ViewInfoContainer(IReadOnlyList<ViewInfo> views)
        {
            ViewInfos = views;
        }

        /// <summary>
        /// The <see cref="IReadOnlyList{T}"/> of <see cref="ViewInfo"/>.
        /// </summary>
        public IReadOnlyList<ViewInfo> ViewInfos { get; }
    }
}
