// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// A base class for building an new <see cref="Endpoint"/>.
    /// </summary>
    public abstract class EndpointBuilder
    {
        /// <summary>
        /// Gets or sets the delegate used to process requests for the endpoint.
        /// </summary>
        public RequestDelegate? RequestDelegate { get; set; }

        /// <summary>
        /// Gets or sets the informational display name of this endpoint.
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// Gets the collection of metadata associated with this endpoint.
        /// </summary>
        public IList<object> Metadata { get; } = new List<object>();

        /// <summary>
        /// Creates an instance of <see cref="Endpoint"/> from the <see cref="EndpointBuilder"/>.
        /// </summary>
        /// <returns>The created <see cref="Endpoint"/>.</returns>
        public abstract Endpoint Build();
    }
}
