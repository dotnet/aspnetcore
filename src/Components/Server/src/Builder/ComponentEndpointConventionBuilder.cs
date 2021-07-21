// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Components.Web;

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

        /// <summary>
        /// Allows JavaScript to add root components dynamically.
        /// </summary>
        /// <param name="configuration">Options specifying which root components may be added from JavaScript.</param>
        /// <param name="defaultMaxInstancesPerType">The maximum number of component instances per type that may be added by JavaScript.</param>
        /// <returns>The <see cref="ComponentEndpointConventionBuilder"/>.</returns>
        public ComponentEndpointConventionBuilder WithJSComponents(
            Action<IJSComponentConfiguration> configuration, int defaultMaxInstancesPerType)
        {
            var jsComponents = new CircuitJSComponentConfiguration
            {
                DefaultMaxInstancesPerType = defaultMaxInstancesPerType
            };

            configuration(jsComponents);

            jsComponents.AddToEndpointMetadata(_hubEndpoint);

            return this;
        }
    }
}
