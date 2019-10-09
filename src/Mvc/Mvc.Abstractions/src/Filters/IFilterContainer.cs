// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Filters
{
    /// <summary>
    /// A filter that requires a reference back to the <see cref="IFilterFactory"/> that created it.
    /// </summary>
    public interface IFilterContainer
    {
        /// <summary>
        /// The <see cref="IFilterFactory"/> that created this filter instance.
        /// </summary>
        IFilterMetadata FilterDefinition { get; set; }
    }
}
