// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

/// <summary>
/// Allows customization of the <see cref="ApplicationModel"/>.
/// </summary>
/// <remarks>
/// Implementations of this interface can be registered in <see cref="MvcOptions.Conventions"/>
/// to customize metadata about the application.
///
/// <see cref="IApplicationModelConvention"/> run before other types of customizations to the
/// reflected model.
/// </remarks>
public interface IApplicationModelConvention
{
    /// <summary>
    /// Called to apply the convention to the <see cref="ApplicationModel"/>.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationModel"/>.</param>
    void Apply(ApplicationModel application);
}
