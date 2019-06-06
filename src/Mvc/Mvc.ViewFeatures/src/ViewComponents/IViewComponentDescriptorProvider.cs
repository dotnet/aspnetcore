// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Mvc.ViewComponents
{
    /// <summary>
    /// Discovers the view components in the application.
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