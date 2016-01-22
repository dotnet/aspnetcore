// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Abstractions;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    /// <summary>
    /// A data structure that retrieves a list of <see cref="ActionDescriptor"/> matches based on the values
    /// supplied for the current request by <see cref="Microsoft.AspNetCore.Routing.RouteData.Values"/>.
    /// </summary>
    public interface IActionSelectionDecisionTree
    {
        /// <summary>
        /// Gets the version. The same as the value of
        /// <see cref="Infrastructure.ActionDescriptorCollection.Version"/>.
        /// </summary>
        int Version { get; }

        /// <summary>
        /// Retrieves a set of <see cref="ActionDescriptor"/> based on the route values supplied by
        /// <paramref name="routeValues"/>/
        /// </summary>
        /// <param name="routeValues">The route values for the current request.</param>
        /// <returns>A set of <see cref="ActionDescriptor"/> matching the route values.</returns>
        IReadOnlyList<ActionDescriptor> Select(IDictionary<string, object> routeValues);
    }
}