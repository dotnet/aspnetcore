// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc.ApplicationModel
{
    /// <summary>
    /// Allows customization of the of the <see cref="GlobalModel"/>.
    /// </summary>
    /// <remarks>
    /// Implementaions of this interface can be registered in <see cref="MvcOptions.ApplicationModelConventions"/>
    /// to customize metadata about the application.
    /// 
    /// <see cref="IGlobalModelConvention"/> run before other types of customizations to the
    /// reflected model.
    /// </remarks>
    public interface IGlobalModelConvention
    {
        /// <summary>
        /// Called to apply the convention to the <see cref="GlobalModel"/>.
        /// </summary>
        /// <param name="model">The <see cref="GlobalModel"/>.</param>
        void Apply([NotNull] GlobalModel model);
    }
}