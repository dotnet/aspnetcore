// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.Razor.OptionDescriptors
{
    /// <summary>
    /// Provides an activated collection of <see cref="IViewLocationExpander"/> instances.
    /// </summary>
    public interface IViewLocationExpanderProvider
    {
        /// <summary>
        /// Gets a collection of activated <see cref="IViewLocationExpander"/> instances.
        /// </summary>
        IReadOnlyList<IViewLocationExpander> ViewLocationExpanders { get; }
    }
}