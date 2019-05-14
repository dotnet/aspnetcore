// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing
{
    /// <summary>
    /// Specifies an endpoint name in <see cref="Endpoint.Metadata"/>.
    /// </summary>
    /// <remarks>
    /// Endpoint names must be unique within an application, and can be used to unambiguously
    /// identify a desired endpoint for URI generation using <see cref="LinkGenerator"/>.
    /// </remarks>
    public class EndpointNameMetadata : IEndpointNameMetadata
    {
        /// <summary>
        /// Creates a new instance of <see cref="EndpointNameMetadata"/> with the provided endpoint name.
        /// </summary>
        /// <param name="endpointName">The endpoint name.</param>
        public EndpointNameMetadata(string endpointName)
        {
            if (endpointName == null)
            {
                throw new ArgumentNullException(nameof(endpointName));
            }

            EndpointName = endpointName;
        }

        /// <summary>
        /// Gets the endpoint name.
        /// </summary>
        public string EndpointName { get; }
    }
}
