// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc.RazorPages
{
    /// <summary>
    /// Provides methods to create a Razor page.
    /// </summary>
    public interface IPageActivatorProvider
    {
        /// <summary>
        /// Creates a Razor page activator.
        /// </summary>
        /// <param name="descriptor">The <see cref="CompiledPageActionDescriptor"/>.</param>
        /// <returns>The delegate used to activate the page.</returns>
        Func<PageContext, object> CreateActivator(CompiledPageActionDescriptor descriptor);

        /// <summary>
        /// Releases a Razor page.
        /// </summary>
        /// <param name="descriptor">The <see cref="CompiledPageActionDescriptor"/>.</param>
        /// <returns>The delegate used to dispose the activated page.</returns>
        Action<PageContext, object> CreateReleaser(CompiledPageActionDescriptor descriptor);
    }
}