// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Builds conventions that will be used for customization of MapAction <see cref="EndpointBuilder"/> instances.
    /// </summary>
    public sealed class MapActionEndpointConventionBuilder : IEndpointConventionBuilder
    {
        private readonly List<IEndpointConventionBuilder> _endpointConventionBuilders;

        internal MapActionEndpointConventionBuilder(IEndpointConventionBuilder endpointConventionBuilder)
        {
            _endpointConventionBuilders = new List<IEndpointConventionBuilder>() { endpointConventionBuilder };
        }

        internal MapActionEndpointConventionBuilder(List<IEndpointConventionBuilder> endpointConventionBuilders)
        {
            _endpointConventionBuilders = endpointConventionBuilders;
        }

        /// <summary>
        /// Adds the specified convention to the builder. Conventions are used to customize <see cref="EndpointBuilder"/> instances.
        /// </summary>
        /// <param name="convention">The convention to add to the builder.</param>
        public void Add(Action<EndpointBuilder> convention)
        {
            foreach (var endpointConventionBuilder in _endpointConventionBuilders)
            {
                endpointConventionBuilder.Add(convention);
            }
        }
    }
}
