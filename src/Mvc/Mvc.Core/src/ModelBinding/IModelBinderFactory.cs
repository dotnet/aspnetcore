// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    /// <summary>
    /// A factory abstraction for creating <see cref="IModelBinder"/> instances.
    /// </summary>
    public interface IModelBinderFactory
    {
        /// <summary>
        /// Creates a new <see cref="IModelBinder"/>.
        /// </summary>
        /// <param name="context">The <see cref="ModelBinderFactoryContext"/>.</param>
        /// <returns>An <see cref="IModelBinder"/> instance.</returns>
        IModelBinder CreateBinder(ModelBinderFactoryContext context);
    }
}
