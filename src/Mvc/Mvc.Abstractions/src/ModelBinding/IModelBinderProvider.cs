// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

/// <summary>
/// Creates <see cref="IModelBinder"/> instances. Register <see cref="IModelBinderProvider"/>
/// instances in <c>MvcOptions</c>.
/// </summary>
public interface IModelBinderProvider
{
    /// <summary>
    /// Creates a <see cref="IModelBinder"/> based on <see cref="ModelBinderProviderContext"/>.
    /// </summary>
    /// <param name="context">The <see cref="ModelBinderProviderContext"/>.</param>
    /// <returns>An <see cref="IModelBinder"/>.</returns>
    IModelBinder? GetBinder(ModelBinderProviderContext context);
}
