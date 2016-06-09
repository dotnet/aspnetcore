// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    /// <summary>
    /// Provides a dictionary of action arguments.
    /// </summary>
    public interface IControllerArgumentBinder
    {
        /// <summary>
        /// Asyncronously binds a dictionary of the parameter-argument name-value pairs,
        /// which can be used to invoke the action. Also binds properties explicitly marked properties on the 
        /// <paramref name="controller"/>.
        /// </summary>
        /// <param name="controllerContext">The <see cref="ControllerContext"/> associated with the current action.</param>
        /// <param name="controller">The controller object which contains the action.</param>
        /// <param name="arguments">The arguments dictionary.</param>
        /// <returns>A <see cref="Task"/> which, when completed signals the completion of argument binding.</returns>
        Task BindArgumentsAsync(
            ControllerContext controllerContext, 
            object controller,
            IDictionary<string, object> arguments);
    }
}
