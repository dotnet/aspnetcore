// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc.ReflectedModelBuilder
{
    /// <summary>
    /// Allows customization of the of the <see cref="ReflectedControllerModel"/>.
    /// </summary>
    /// <remarks>
    /// To use this interface, create an <see cref="System.Attribute"/> class which implements the interface and
    /// place it on an action method parameter.
    /// 
    /// <see cref="IReflectedParameterModelConvention"/> customizations run after 
    /// <see cref="IReflectedActionModelConvention"/> customizations.
    /// </remarks>
    public interface IReflectedParameterModelConvention
    {
        /// <summary>
        /// Called to apply the convention to the <see cref="ReflectedParameterModel"/>.
        /// </summary>
        /// <param name="model">The <see cref="ReflectedParameterModel"/>.</param>
        void Apply([NotNull] ReflectedParameterModel model);
    }
}