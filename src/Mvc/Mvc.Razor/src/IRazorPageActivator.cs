// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.Rendering;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    /// <summary>
    /// Provides methods to activate properties on a <see cref="IRazorPage"/> instance.
    /// </summary>
    public interface IRazorPageActivator
    {
        /// <summary>
        /// When implemented in a type, activates an instantiated page.
        /// </summary>
        /// <param name="page">The page to activate.</param>
        /// <param name="context">The <see cref="ViewContext"/> for the executing view.</param>
        void Activate(IRazorPage page, ViewContext context);
    }
}