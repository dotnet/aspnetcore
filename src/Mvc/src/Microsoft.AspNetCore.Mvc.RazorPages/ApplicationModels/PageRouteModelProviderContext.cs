// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Hosting;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    /// <summary>
    /// A context object for <see cref="IPageRouteModelProvider"/>.
    /// </summary>
    public class PageRouteModelProviderContext
    {
        /// <summary>
        /// Creates a new instance of <see cref="PageRouteModelProviderContext"/>.
        /// </summary>
        public PageRouteModelProviderContext()
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="PageRouteModelProviderContext"/>.
        /// </summary>
        /// <param name="compiledItems">The list of compiled items.</param>
        public PageRouteModelProviderContext(IEnumerable<RazorCompiledItem> compiledItems)
        {
            if (compiledItems == null)
            {
                throw new ArgumentNullException(nameof(compiledItems));
            }

            CompiledItems = compiledItems.ToList();
        }

        /// <summary>
        /// Gets the list of compiled items.
        /// </summary>
        public IReadOnlyList<RazorCompiledItem> CompiledItems { get; }

        /// <summary>
        /// Gets the <see cref="PageRouteModel"/> instances.
        /// </summary>
        public IList<PageRouteModel> RouteModels { get; } = new List<PageRouteModel>();
    }
}