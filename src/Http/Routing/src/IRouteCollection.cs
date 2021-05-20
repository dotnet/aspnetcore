// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Routing
{
    /// <summary>
    /// Interface for a router that supports appending new routes.
    /// </summary>
    public interface IRouteCollection : IRouter
    {
        /// <summary>
        /// Appends the collection of routes defined in <paramref name="router"/>.
        /// </summary>
        /// <param name="router">A <see cref="IRouter"/> instance.</param>
        void Add(IRouter router);
    }
}
