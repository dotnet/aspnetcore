// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Builds conventions that will be used for customization of ComponentHub <see cref="EndpointBuilder"/> instances.
    /// </summary>
    public sealed class ComponentEndpointConventionBuilder : IHubEndpointConventionBuilder
    {
        private readonly IEndpointConventionBuilder _hubEndpoint;
        private readonly IEndpointConventionBuilder _disconnectEndpoint;

        internal ComponentEndpointConventionBuilder(IEndpointConventionBuilder hubEndpoint, IEndpointConventionBuilder disconnectEndpoint)
        {
            _hubEndpoint = hubEndpoint;
            _disconnectEndpoint = disconnectEndpoint;
        }

        /// <summary>
        /// Adds the specified convention to the builder. Conventions are used to customize <see cref="EndpointBuilder"/> instances.
        /// </summary>
        /// <param name="convention">The convention to add to the builder.</param>
        public void Add(Action<EndpointBuilder> convention)
        {
            _hubEndpoint.Add(convention);
            _disconnectEndpoint.Add(convention);
        }
    }
}
