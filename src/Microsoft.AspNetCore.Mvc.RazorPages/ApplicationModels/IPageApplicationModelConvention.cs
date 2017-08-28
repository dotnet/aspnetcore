// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    /// <summary>
    /// Allows customization of the <see cref="PageApplicationModel"/>.
    /// </summary>
    public interface IPageApplicationModelConvention : IPageConvention
    {
        /// <summary>
        /// Called to apply the convention to the <see cref="PageApplicationModel"/>.
        /// </summary>
        /// <param name="model">The <see cref="PageApplicationModel"/>.</param>
        void Apply(PageApplicationModel model);
    }
}
