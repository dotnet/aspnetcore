// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;

namespace Microsoft.AspNetCore.Components.Server
{
    /// <summary>
    /// Builds conventions that will be used for customization of ComponentHub <see cref="EndpointBuilder"/> instances.
    /// </summary>
    public sealed class ComponentEndpointConventionBuilder : IHubEndpointConventionBuilder
    {
        private readonly IEndpointConventionBuilder[] _endpointConventionBuilders;

        internal ComponentEndpointConventionBuilder(params IEndpointConventionBuilder[] endpointConventionBuilders)
        {
            _endpointConventionBuilders = endpointConventionBuilders;
        }

        /// <summary>
        /// Adds the specified convention to the builder. Conventions are used to customize <see cref="EndpointBuilder"/> instances.
        /// </summary>
        /// <param name="convention">The convention to add to the builder.</param>
        public void Add(Action<EndpointBuilder> convention)
        {
            for (int i = 0; i < _endpointConventionBuilders.Length; i++)
            {
                _endpointConventionBuilders[i].Add(convention);
            }
        }
    }
}
