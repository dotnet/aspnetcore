// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing
{
    /// <summary>
    /// Specifies an endpoint group name in <see cref="Endpoint.Metadata"/>.
    /// </summary>
    public class EndpointGroupNameMetadata : IEndpointGroupNameMetadata
    {
        /// <summary>
        /// Creates a new instance of <see cref="EndpointGroupNameMetadata"/> with the provided endpoint name.
        /// </summary>
        /// <param name="endpointGroupName">The endpoint name.</param>
        public EndpointGroupNameMetadata(string endpointGroupName)
        {
            if (endpointGroupName == null)
            {
                throw new ArgumentNullException(nameof(endpointGroupName));
            }

            EndpointGroupName = endpointGroupName;
        }

        /// <summary>
        /// Gets the endpoint name.
        /// </summary>
        public string EndpointGroupName { get; }
    }
}
