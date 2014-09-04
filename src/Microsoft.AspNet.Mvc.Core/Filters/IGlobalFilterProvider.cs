// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.Filters
{
    /// <summary>
    /// Provides access to the collection of <see cref="IFilter"/> for globally registered filters.
    /// </summary>
    public interface IGlobalFilterProvider
    {
        /// <summary>
        /// Gets the collection of <see cref="IFilter"/>.
        /// </summary>
        IReadOnlyList<IFilter> Filters { get; }
    }
}