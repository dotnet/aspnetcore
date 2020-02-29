// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
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
        void Apply(ParameterModel parameter);
    }
}
