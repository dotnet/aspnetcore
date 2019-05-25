// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
        /// Maximum number of queued requests before the server starts rejecting connections.
        /// The server will respond with a 503 if this limit is exceeeded.
        /// </summary>
        public int RequestQueueLimit { get; set; } = 5000;
    }
}
