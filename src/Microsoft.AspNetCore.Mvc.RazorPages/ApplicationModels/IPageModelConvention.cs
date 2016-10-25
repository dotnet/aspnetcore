// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    /// <summary>
    /// Allows customization of the of the <see cref="PageModel"/>.
    /// </summary>
    public interface IPageModelConvention
    {
        /// <summary>
        /// Called to apply the convention to the <see cref="PageModel"/>.
        /// </summary>
        /// <param name="model">The <see cref="PageModel"/>.</param>
        void Apply(PageModel model);
    }
}
