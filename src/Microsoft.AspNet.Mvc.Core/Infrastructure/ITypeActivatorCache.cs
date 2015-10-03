// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Mvc.Infrastructure
{
    /// <summary>
    /// Caches <see cref="Microsoft.Extensions.DependencyInjection.ObjectFactory"/> instances produced by 
    /// <see cref="Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateFactory(Type, Type[])"/>.
    /// </summary>
    public interface ITypeActivatorCache
    {
        /// <summary>
        /// Creates an instance of <typeparamref name="TInstance"/>.
        /// </summary>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/> used to resolve dependencies for
        /// <paramref name="implementationType"/>.</param>
        /// <param name="implementationType">The <see cref="Type"/> of the <typeparamref name="TInstance"/> to create.</param>
        TInstance CreateInstance<TInstance>(IServiceProvider serviceProvider, Type optionType);
    }
}