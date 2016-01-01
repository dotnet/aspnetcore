// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Routing;

namespace Microsoft.AspNet.Mvc.Infrastructure
{
    /// <summary>
    /// Defines an interface for selecting an MVC action to invoke for the current request.
    /// </summary>
    public interface IActionSelector
    {
        /// <summary>
        /// Selects an <see cref="ActionDescriptor"/> for the request associated with <paramref name="context"/>.
        /// </summary>
        /// <param name="context">The <see cref="RouteContext"/> for the current request.</param>
        /// <returns>An <see cref="ActionDescriptor"/> or <c>null</c> if no action can be selected.</returns>
        /// <exception cref="AmbiguousActionException">
        /// Thrown when action selection results in an ambiguity.
        /// </exception>
        ActionDescriptor Select(RouteContext context);
    }
}
