// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
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
}
