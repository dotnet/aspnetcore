// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// Represents a logical endpoint in an application.
    /// </summary>
    public class Endpoint
    {
        /// <summary>
        /// Creates a new instance of <see cref="Endpoint"/>.
        /// </summary>
        /// <param name="requestDelegate">The delegate used to process requests for the endpoint.</param>
        /// <param name="metadata">
        /// The endpoint <see cref="EndpointMetadataCollection"/>. May be null.
        /// </param>
        /// <param name="displayName">
        /// The informational display name of the endpoint. May be null.
        /// </param>
        public Endpoint(
            RequestDelegate requestDelegate,
            EndpointMetadataCollection metadata,
            string displayName)
        {
            // All are allowed to be null
            RequestDelegate = requestDelegate;
            Metadata = metadata ?? EndpointMetadataCollection.Empty;
            DisplayName = displayName;
        }

        /// <summary>
        /// Gets the informational display name of this endpoint.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the collection of metadata associated with this endpoint.
        /// </summary>
        public EndpointMetadataCollection Metadata { get; }

        /// <summary>
        /// Gets the delegate used to process requests for the endpoint.
        /// </summary>
        public RequestDelegate RequestDelegate { get; }

        public override string ToString() => DisplayName ?? base.ToString();
    }
}
