// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.ViewComponents
{
    /// <summary>
    /// Selects a view component based on a view component name.
    /// </summary>
    public interface IViewComponentSelector
    {
        /// <summary>
        /// Selects a view component based on <paramref name="componentName"/>.
        /// </summary>
        /// <param name="componentName">The view component name.</param>
        /// <returns>A <see cref="ViewComponentDescriptor"/>, or <c>null</c> if no match is found.</returns>
        ViewComponentDescriptor SelectComponent(string componentName);
    }
}
