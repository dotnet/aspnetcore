// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing
{
    /// <summary>
    /// Defines a contract use to specify an endpoint name in <see cref="Endpoint.Metadata"/>.
    /// </summary>
    /// <remarks>
    /// Endpoint names must be unique within an application, and can be used to unambiguously
    /// identify a desired endpoint for URI generation using <see cref="LinkGenerator"/>.
    /// </remarks>
    public interface IEndpointNameMetadata
    {
        /// <summary>
        /// Gets the endpoint name.
        /// </summary>
        string EndpointName { get; }
    }
}
