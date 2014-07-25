// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// Provides an activated collection of <see cref="IValueProviderFactory"/> instances.
    /// </summary>
    public interface IValueProviderFactoryProvider
    {
        /// <summary>
        /// Gets a collection of activated IValueProviderFactory instances.
        /// </summary>
        IReadOnlyList<IValueProviderFactory> ValueProviderFactories { get; }
    }
}