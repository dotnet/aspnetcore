// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

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
