// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Http.Connections
{
    /// <summary>
    /// Stores information used by the client to start a real-time connection with the endpoint.
    /// </summary>
    public class NegotiationResponse
    {
        /// <summary>
        /// If this has a value then the client should do another negotiate request with the <see cref="Url"/> and <see cref="AccessToken"/>.
        /// </summary>
        public string Url { get; set; }
        /// <summary>
        /// Used with <see cref="Url"/> to negotiate with a different endpoint.
        /// </summary>
        public string AccessToken { get; set; }
        /// <summary>
        /// The ID query string value to be used when connecting with the endpoint.
        /// </summary>
        public string ConnectionId { get; set; }
        /// <summary>
        /// A list of <see cref="AvailableTransport"/> that the client can choose from to connect with.
        /// </summary>
        public IList<AvailableTransport> AvailableTransports { get; set; }
        /// <summary>
        /// If this has a value then the negotiate failed and this value should have some details on why.
        /// </summary>
        public string Error { get; set; }
    }
}
