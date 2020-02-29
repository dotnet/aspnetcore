// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    /// <summary>
    /// A context object for <see cref="IPageRouteModelProvider"/>.
    /// </summary>
    public class PageRouteModelProviderContext
    {
        /// <summary>
        /// Gets the <see cref="PageRouteModel"/> instances.
        /// </summary>
        public IList<PageRouteModel> RouteModels { get; } = new List<PageRouteModel>();
    }
}