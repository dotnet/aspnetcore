// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.SignalR
{
    /// <summary>
    /// Builds conventions that will be used for customization of Hub <see cref="EndpointBuilder"/> instances.
    /// </summary>
    public class HubEndpointConventionBuilder : IEndpointConventionBuilder
    {
        private readonly IEndpointConventionBuilder _endpointConventionBuilder;

        /// <summary>
        /// Initializes a new instance of the <see cref="HubEndpointConventionBuilder"/> class.
        /// </summary>
        /// <param name="endpointConventionBuilder">The nested convention builder.</param>
        public HubEndpointConventionBuilder(IEndpointConventionBuilder endpointConventionBuilder)
        {
            _endpointConventionBuilder = endpointConventionBuilder;
        }

        /// <summary>
        /// Adds the specified convention to the builder. Conventions are used to customize <see cref="EndpointBuilder"/> instances.
        /// </summary>
        /// <param name="convention">The convention to add to the builder.</param>
        public void Add(Action<EndpointBuilder> convention)
        {
            _endpointConventionBuilder.Add(convention);
        }
    }
}
