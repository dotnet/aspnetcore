// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc.RazorPages
{
    /// <summary>
    /// Provides methods for creation and disposal of Razor Page models.
    /// </summary>
    public interface IPageModelFactoryProvider
    {
        /// <summary>
        /// Creates a factory for producing models for Razor Pages given the specified <see cref="PageContext"/>.
        /// </summary>
        /// <param name="descriptor">The <see cref="CompiledPageActionDescriptor"/>.</param>
        /// <returns>The Razor Page model factory.</returns>
        Func<PageContext, object> CreateModelFactory(CompiledPageActionDescriptor descriptor);

        /// <summary>
        /// Releases a Razor Page model.
        /// </summary>
        /// <param name="descriptor">The <see cref="CompiledPageActionDescriptor"/>.</param>
        /// <returns>The delegate used to release the created Razor Page model.</returns>
        Action<PageContext, object> CreateModelDisposer(CompiledPageActionDescriptor descriptor);
    }
}