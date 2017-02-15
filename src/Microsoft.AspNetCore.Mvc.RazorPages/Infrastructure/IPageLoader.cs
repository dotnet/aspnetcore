// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    /// <summary>
    /// Creates a <see cref="CompiledPageActionDescriptor"/> from a <see cref="PageActionDescriptor"/>.
    /// </summary>
    public interface IPageLoader
    {
        /// <summary>
        /// Produces a <see cref="CompiledPageActionDescriptor"/> given a <see cref="PageActionDescriptor"/>.
        /// </summary>
        /// <param name="actionDescriptor">The <see cref="PageActionDescriptor"/>.</param>
        /// <returns>The <see cref="CompiledPageActionDescriptor"/>.</returns>
        CompiledPageActionDescriptor Load(PageActionDescriptor actionDescriptor);
    }
}
