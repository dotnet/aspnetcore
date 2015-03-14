// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.ViewComponents
{
    /// <summary>
    /// Discovers the View Components in the application.
    /// </summary>
    public interface IViewComponentDescriptorProvider
    {
        /// <summary>
        /// Gets the set of <see cref="ViewComponentDescriptor"/>.
        /// </summary>
        /// <returns>A list of <see cref="ViewComponentDescriptor"/>.</returns>
        IEnumerable<ViewComponentDescriptor> GetViewComponents();
    }
}