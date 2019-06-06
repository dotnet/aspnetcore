// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Mvc.ViewEngines
{
    /// <summary>
    /// Represents an <see cref="IViewEngine"/> that delegates to one of a collection of view engines.
    /// </summary>
    public interface ICompositeViewEngine : IViewEngine
    {
        /// <summary>
        /// Gets the list of <see cref="IViewEngine"/> this instance of <see cref="ICompositeViewEngine"/> delegates
        /// to.
        /// </summary>
        IReadOnlyList<IViewEngine> ViewEngines { get; }
    }
}