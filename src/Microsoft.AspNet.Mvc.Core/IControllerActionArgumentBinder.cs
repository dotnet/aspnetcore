// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Provides a dictionary of action arguments.
    /// </summary>
    public interface IControllerActionArgumentBinder
    {
        /// <summary>
        /// Returns a dictionary of representing the parameter-argument name-value pairs,
        /// which can be used to invoke the action.
        /// </summary>
        /// <param name="context">The action context assoicated with the current action.</param>
        Task<IDictionary<string, object>> GetActionArgumentsAsync([NotNull] ActionContext context);
    }
}
