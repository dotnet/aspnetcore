// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    /// <summary>
    /// Caches <see cref="Extensions.DependencyInjection.ObjectFactory"/> instances produced by
    /// <see cref="Extensions.DependencyInjection.ActivatorUtilities.CreateFactory(Type, Type[])"/>.
    /// </summary>
    internal interface ITypeActivatorCache
    {
        /// <summary>
        /// Creates an instance of <typeparamref name="TInstance"/>.
        /// </summary>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/> used to resolve dependencies for
        /// <paramref name="optionType"/>.</param>
        /// <param name="optionType">The <see cref="Type"/> of the <typeparamref name="TInstance"/> to create.</param>
        TInstance CreateInstance<TInstance>(IServiceProvider serviceProvider, Type optionType);
    }
}
