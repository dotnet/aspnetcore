// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.Rendering
{
    /// <summary>
    /// Provides an activated collection of <see cref="IViewEngine"/> instances.
    /// </summary>
    public interface IViewEngineProvider
    {
        /// <summary>
        /// Gets a collection of activated IViewEngine instances.
        /// </summary>
        IReadOnlyList<IViewEngine> ViewEngines { get; }
    }
}
