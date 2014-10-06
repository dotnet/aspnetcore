// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc.ApplicationModel
{
    /// <summary>
    /// Allows customization of the of the <see cref="ControllerModel"/>.
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
        /// <param name="model">The <see cref="ParameterModel"/>.</param>
        void Apply([NotNull] ParameterModel model);
    }
}