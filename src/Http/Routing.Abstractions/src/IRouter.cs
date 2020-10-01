// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Routing
{
    /// <summary>
    /// Interface for implementing a router.
    /// </summary>
    public interface IRouter
    {
        /// <summary>
        /// Asynchronously routes given the current context.
        /// </summary>
        /// <param name="context">A <see cref="RouteContext"/> instance.</param>
        Task RouteAsync(RouteContext context);

        /// <summary>
        /// Creates a <see cref="VirtualPathData"/> from the given <see cref="VirtualPathContext"/>.
        /// </summary>
        /// <param name="context">A <see cref="VirtualPathContext"/> instance.</param>
        /// <returns>A <see cref="VirtualPathData"/> instance.</returns>
        VirtualPathData? GetVirtualPath(VirtualPathContext context);
    }
}
