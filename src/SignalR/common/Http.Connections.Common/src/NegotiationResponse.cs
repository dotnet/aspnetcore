// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Http.Connections
{
    /// <summary>
    /// A response to a '/negotiate' request.
    /// </summary>
    public class NegotiationResponse
    {
        /// <summary>
        /// An optional Url to redirect the client to another endpoint.
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        /// An optional access token to go along with the Url.
        /// </summary>
        public string? AccessToken { get; set; }

        /// <summary>
        /// The public ID for the connection.
        /// </summary>
        public string? ConnectionId { get; set; }

        /// <summary>
        /// The private ID for the connection.
        /// </summary>
        public string? ConnectionToken { get; set; }

        /// <summary>
        /// The minimum value between the version the client sends and the maximum version the server supports.
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// A list of transports the server supports.
        /// </summary>
        public IList<AvailableTransport>? AvailableTransports { get; set; }

        /// <summary>
        /// An optional error during the negotiate. If this is not null the other properties on the response can be ignored.
        /// </summary>
        public string? Error { get; set; }
    }
}
