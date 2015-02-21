// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ApplicationModels
{
    /// <summary>
    /// Allows customization of the of the <see cref="ApplicationModel"/>.
    /// </summary>
    /// <remarks>
    /// Implementaions of this interface can be registered in <see cref="MvcOptions.Conventions"/>
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
        void Apply([NotNull] ApplicationModel application);
    }
}