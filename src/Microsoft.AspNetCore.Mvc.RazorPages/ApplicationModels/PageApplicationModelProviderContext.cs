// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    /// <summary>
    /// A context object for <see cref="IPageApplicationModelProvider"/>.
    /// </summary>
    public class PageApplicationModelProviderContext
    {
        /// <summary>
        /// Gets the <see cref="PageApplicationModel"/> instances.
        /// </summary>
        public IList<PageApplicationModel> Results { get; } = new List<PageApplicationModel>();
    }
}