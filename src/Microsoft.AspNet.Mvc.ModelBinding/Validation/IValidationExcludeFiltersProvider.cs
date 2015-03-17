// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    /// <summary>
    /// Provides an activated collection of <see cref="IExcludeTypeValidationFilter"/> instances.
    /// </summary>
    public interface IValidationExcludeFiltersProvider
    {
        /// <summary>
        /// Gets a collection of activated <see cref="IExcludeTypeValidationFilter"/> instances.
        /// </summary>
        IReadOnlyList<IExcludeTypeValidationFilter> ExcludeFilters { get; }
    }
}
