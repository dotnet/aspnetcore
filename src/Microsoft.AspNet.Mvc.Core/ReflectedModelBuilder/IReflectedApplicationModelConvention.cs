// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc.ReflectedModelBuilder
{
    /// <summary>
    /// Allows customization of the of the <see cref="ReflectedApplicationModel"/>.
    /// </summary>
    /// <remarks>
    /// Implementaions of this interface can be registered in <see cref="MvcOptions.ApplicationModelConventions"/>
    /// to customize metadata about the application.
    /// 
    /// <see cref="IReflectedApplicationModelConvention"/> run before other types of customizations to the
    /// reflected model.
    /// </remarks>
    public interface IReflectedApplicationModelConvention
    {
        /// <summary>
        /// Called to apply the convention to the <see cref="ReflectedApplicationModel"/>.
        /// </summary>
        /// <param name="model">The <see cref="ReflectedApplicationModel"/>.</param>
        void Apply([NotNull] ReflectedApplicationModel model);
    }
}