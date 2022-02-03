// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

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
