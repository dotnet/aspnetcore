// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RequestThrottling;

namespace Microsoft.AspNetCore.RequestThrottling
{
    /// <summary>
    /// Specifies options for the <see cref="RequestThrottlingMiddleware"/>.
    /// </summary>
    public class RequestThrottlingOptions
    {
        /// <summary>
        /// Maximum number of concurrent requests. Any extras will be queued on the server. 
        /// This is null by default because the correct value is application specific. This option must be configured by the application.
        /// </summary>
        public int? MaxConcurrentRequests { get; set; }

        /// <summary>
        /// Maximum number of queued requests before the server starts rejecting connections with '503 Service Unavailible'.
        /// Setting this value to 0 will disable the queue; all requests will either immediately enter the server or be rejected.
        /// Defaults to 5000 queued requests.
        /// </summary>
        public int RequestQueueLimit { get; set; } = 5000;

        /// <summary>
        /// A <see cref="RequestDelegate"/> that handles requests rejected by this middleware.
        /// If it doesn't modify the response, an empty 503 response will be written.
        /// </summary>
        public RequestDelegate OnRejected { get; set; } = context =>
        {
            return Task.CompletedTask;
        };

        /// <summary>
        /// For internal testing only. If true, no requests will enter the server.
        /// </summary>
        internal bool ServerAlwaysBlocks { get; set; } = false;
    }
}
