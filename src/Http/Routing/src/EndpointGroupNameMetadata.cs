// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing
{
    /// <summary>
    /// Specifies an endpoint group name in <see cref="Microsoft.AspNetCore.Http.Endpoint.Metadata"/>.
    /// </summary>
    public class EndpointGroupNameMetadata : IEndpointGroupNameMetadata
    {
        /// <summary>
        /// Creates a new instance of <see cref="EndpointGroupNameMetadata"/> with the provided endpoint group name.
        /// </summary>
        /// <param name="endpointGroupName">The endpoint group name.</param>
        public EndpointGroupNameMetadata(string endpointGroupName)
        {
            if (endpointGroupName == null)
            {
                throw new ArgumentNullException(nameof(endpointGroupName));
            }

            EndpointGroupName = endpointGroupName;
        }

        /// <summary>
        /// Gets the endpoint group name.
        /// </summary>
        public string EndpointGroupName { get; }
    }
}
