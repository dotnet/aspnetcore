// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

/// <summary>
/// Allows customization of the properties and parameters on controllers and Razor Pages.
/// </summary>
/// <remarks>
/// To use this interface, create an <see cref="System.Attribute"/> class which implements the interface and
/// place it on an action method parameter.
/// </remarks>
public interface IParameterModelBaseConvention
{
    /// <summary>
    /// Called to apply the convention to the <see cref="ParameterModelBase"/>.
    /// </summary>
    /// <param name="parameter">The <see cref="ParameterModelBase"/>.</param>
    void Apply(ParameterModelBase parameter);
}
