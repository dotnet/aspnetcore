// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc.ReflectedModelBuilder
{
    /// <summary>
    /// Allows customization of the of the <see cref="ReflectedActionModel"/>.
    /// </summary>
    /// <remarks>
    /// To use this interface, create an <see cref="System.Attribute"/> class which implements the interface and
    /// place it on an action method.
    /// 
    /// <see cref="IReflectedActionModelConvention"/> customizations run after 
    /// <see cref="IReflectedActionModelConvention"/> customications and before 
    /// <see cref="IReflectedParameterModelConvention"/> customizations.
    /// </remarks>
    public interface IReflectedActionModelConvention
    {
        /// <summary>
        /// Called to apply the convention to the <see cref="ReflectedActionModel"/>.
        /// </summary>
        /// <param name="model">The <see cref="ReflectedActionModel"/>.</param>
        void Apply([NotNull] ReflectedActionModel model);
    }
}