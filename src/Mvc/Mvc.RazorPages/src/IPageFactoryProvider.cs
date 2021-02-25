// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Microsoft.AspNetCore.Mvc.RazorPages
{
    /// <summary>
    /// Provides methods for creation and disposal of Razor pages.
    /// </summary>
    public interface IPageFactoryProvider
    {
        /// <summary>
        /// Creates a factory for producing Razor pages for the specified <see cref="PageContext"/>.
        /// </summary>
        /// <param name="descriptor">The <see cref="CompiledPageActionDescriptor"/>.</param>
        /// <returns>The Razor page factory.</returns>
        Func<PageContext, ViewContext, object> CreatePageFactory(CompiledPageActionDescriptor descriptor);

        /// <summary>
        /// Releases a Razor page.
        /// </summary>
        /// <param name="descriptor">The <see cref="CompiledPageActionDescriptor"/>.</param>
        /// <returns>The delegate used to release the created page.</returns>
        Action<PageContext, ViewContext, object> CreatePageDisposer(CompiledPageActionDescriptor descriptor);

        /// <summary>
        /// Releases a Razor page asynchronously.
        /// </summary>
        /// <param name="descriptor">The <see cref="CompiledPageActionDescriptor"/>.</param>
        /// <returns>The delegate used to release the created page asynchronously.</returns>
        Func<PageContext, ViewContext, object, ValueTask> CreateAsyncPageDisposer(CompiledPageActionDescriptor descriptor)
        {
            var disposer = CreatePageDisposer(descriptor);

            return (context, viewContext, page) =>
            {
                disposer(context, viewContext, page);
                return default;
            };
        }
    }
}
