// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.RequestThrottling
{
    public class RequestThrottlingOptions
    {
        /// <summary>
        /// Maximum number of concurrent requests. Any extras will be queued on the server. 
        /// </summary>
        public int MaxConcurrentRequests { get; set; } = 10;
    }
}
