// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Routing
{
    /// <summary>
    /// Interface for attributes which can supply a route pattern for attribute routing.
    /// </summary>
    public interface IRoutePatternMetadata
    {
        /// <summary>
        /// The route pattern. May be <see langword="null"/>.
        /// </summary>
        string? RoutePattern { get; }
    }
}
