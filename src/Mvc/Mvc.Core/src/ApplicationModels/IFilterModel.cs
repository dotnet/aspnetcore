// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    /// <summary>
    /// Model that has a list of <see cref="IFilterMetadata"/>.
    /// </summary>
    public interface IFilterModel
    {
        /// <summary>
        /// List of <see cref="IFilterMetadata"/>.
        /// </summary>
        IList<IFilterMetadata> Filters { get; }
    }
}
