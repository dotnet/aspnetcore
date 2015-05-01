// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ViewComponents
{
    /// <summary>
    /// Selects a View Component based on a View Component name.
    /// </summary>
    public interface IViewComponentSelector
    {
        /// <summary>
        /// Selects a View Component based on <paramref name="componentName"/>.
        /// </summary>
        /// <param name="componentName">The View Component name.</param>
        /// <returns>A <see cref="ViewComponentDescriptor"/>, or <c>null</c> if no match is found.</returns>
        ViewComponentDescriptor SelectComponent([NotNull] string componentName);
    }
}
