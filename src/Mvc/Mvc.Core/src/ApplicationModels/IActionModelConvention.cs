// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

/// <summary>
/// Allows customization of the <see cref="ActionModel"/>.
/// </summary>
/// <remarks>
/// To use this interface, create an <see cref="System.Attribute"/> class which implements the interface and
/// place it on an action method.
///
/// <see cref="IActionModelConvention"/> customizations run after
/// <see cref="IControllerModelConvention"/> customizations and before
/// <see cref="IParameterModelConvention"/> customizations.
/// </remarks>
public interface IActionModelConvention
{
    /// <summary>
    /// Called to apply the convention to the <see cref="ActionModel"/>.
    /// </summary>
    /// <param name="action">The <see cref="ActionModel"/>.</param>
    void Apply(ActionModel action);
}
