// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

/// <summary>
/// Allows customization of the <see cref="ParameterModel"/>.
/// </summary>
/// <remarks>
/// To use this interface, create an <see cref="System.Attribute"/> class which implements the interface and
/// place it on an action method parameter.
///
/// <see cref="IParameterModelConvention"/> customizations run after
/// <see cref="IActionModelConvention"/> customizations.
/// </remarks>
public interface IParameterModelConvention
{
    /// <summary>
    /// Called to apply the convention to the <see cref="ParameterModel"/>.
    /// </summary>
    /// <param name="parameter">The  <see cref="ParameterModel"/>.</param>
    void Apply(ParameterModel parameter);
}
