// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    /// <summary>
    /// Provides a predicate which can determines which model properties should be bound by model binding.
    /// </summary>
    public interface IPropertyFilterProvider
    {
        /// <summary>
        /// Gets a predicate which can determines which model properties should be bound by model binding.
        /// </summary>
        Func<ModelMetadata, bool> PropertyFilter { get; }
    }
}
